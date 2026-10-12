using System.Net;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.HealthChecks;

public class HealthCheckEndpointPathTests
{
    // The paths the Container Apps probes and the APIM availability test point at:
    // .azure/modules/containerApp/main.bicep and .azure/infrastructure/main.bicep. Moving one of
    // these needs an infrastructure change in the same deploy, so they are pinned here.
    [Theory]
    [InlineData("/health/liveness")]
    [InlineData("/health/readiness")]
    [InlineData("/health/startup")]
    [InlineData("/health/deep")]
    [InlineData("/health")]
    public Task Probe_Paths_Should_Be_Mapped(string path) =>
        WithHealthEndpoints(async client =>
        {
            using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        });

    [Fact]
    public Task Liveness_Should_Not_Move_To_The_Library_Default() =>
        WithHealthEndpoints(async client =>
        {
            // The library defaults liveness to /alive as of 0.3.0. We pin /health/liveness instead,
            // so /alive staying unmapped is the signal that the pin is still in effect.
            using var response = await client.GetAsync(new Uri("/alive", UriKind.Relative), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("/health/liveness", DialogportenHealthCheckExtensions.LivenessPath);
        });

    private static async Task WithHealthEndpoints(Func<HttpClient, Task> assert)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDialogportenHealthChecks(
            builder.Configuration,
            DialogportenHealthCheckExtensions.ProbeSectionName);

        await using var app = builder.Build();
        app.MapDialogportenHealthChecks();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        await assert(client);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
