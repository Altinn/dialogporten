using Altinn.AspNet.HealthChecks;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.HealthChecks;

public class HealthCheckResponseDetailTests
{
    // MassTransit's bus-state check publishes the Service Bus host address and queue names through
    // an entry's data, while healthy, and MassTransit cannot be told not to.
    private const string BrokerAddressInData = "sb://dialogporten-prod.servicebus.windows.net/some-queue";

    // Health check exceptions carry hostnames and connection strings, and /health/deep is reachable
    // through APIM, so the message must not reach the response body outside development.
    private const string SecretInExceptionMessage = "Host=db.internal;Password=supersecret";

    // "prod", "staging" and "yt01" are the ASPNETCORE_ENVIRONMENT values our container apps
    // actually run with (.azure/applications/*/[env].bicepparam). None of them is the literal
    // "Production" the library's own detail-level derivation looks for, so they are the cases that
    // would leak if MapDialogportenHealthChecks stopped setting the level explicitly.
    [Theory]
    [InlineData("Production", false)]
    [InlineData("prod", false)]
    [InlineData("staging", false)]
    [InlineData("yt01", false)]
    [InlineData("Development", true)]
    public async Task Deep_Endpoint_Should_Only_Include_Details_In_Development(
        string environment,
        bool expectDetails)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment
        });

        builder.WebHost.UseTestServer();
        builder.Services
            .AddAltinnHealthChecks()
            .AddCheck(
                "throws",
                () => throw new InvalidOperationException(SecretInExceptionMessage),
                tags: [HealthCheckTags.Dependencies])
            .AddCheck(
                "servicebus",
                () => HealthCheckResult.Healthy(
                    "Ready",
                    new Dictionary<string, object> { ["Endpoints"] = BrokerAddressInData }),
                tags: [HealthCheckTags.Dependencies]);

        await using var app = builder.Build();
        app.MapDialogportenHealthChecks();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        using var response = await client.GetAsync(new Uri("/health/deep", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectDetails, body.Contains(SecretInExceptionMessage, StringComparison.Ordinal));
        Assert.Equal(expectDetails, body.Contains(BrokerAddressInData, StringComparison.Ordinal));

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
