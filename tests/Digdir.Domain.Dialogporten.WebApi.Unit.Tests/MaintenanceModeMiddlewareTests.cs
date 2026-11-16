using System.Net;
using AwesomeAssertions;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class MaintenanceModeMiddlewareTests
{
    private const string ApiPath = "/api/v1/serviceowner/dialogs";
    private const string AllowedPath = "/allowed-during-maintenance";
    private static readonly DateTimeOffset RetryAt = new(2026, 11, 15, 22, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("/health/startup")]
    [InlineData("/health/liveness")]
    [InlineData("/health/readiness")]
    [InlineData("/health")]
    [InlineData("/health/deep")]
    [InlineData("/dialogporten/health/liveness")]
    public Task Health_Endpoints_Should_Answer_During_Maintenance(string path) =>
        WithApp(maintenanceEnabled: true, async client =>
        {
            using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        });

    [Fact]
    public Task Endpoints_Allowed_During_Maintenance_Should_Answer_During_Maintenance() =>
        WithApp(maintenanceEnabled: true, async client =>
        {
            using var response = await client.GetAsync(new Uri(AllowedPath, UriKind.Relative), TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        });

    [Theory]
    [InlineData(ApiPath)]
    [InlineData("/dialogporten" + ApiPath)]
    public Task Api_Paths_Should_Get_Service_Unavailable_During_Maintenance(string path) =>
        WithApp(maintenanceEnabled: true, async client =>
        {
            using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter.Should().NotBeNull();
            response.Headers.RetryAfter?.Date.Should().Be(RetryAt);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
                .Should().Be("Service Unavailable");
        });

    [Fact]
    public Task Api_Paths_Should_Answer_Outside_Maintenance() =>
        WithApp(maintenanceEnabled: false, async client =>
        {
            using var response = await client.GetAsync(new Uri(ApiPath, UriKind.Relative), TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        });

    // Mirrors the order in the WebApi and GraphQL hosts: health endpoints mapped first, maintenance
    // mode registered with the other middleware, API endpoints mapped after it. Every app.Use*
    // middleware runs before any mapped endpoint, whatever order the Map* calls come in.
    private static async Task WithApp(bool maintenanceEnabled, Func<HttpClient, Task> assert)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{WebHostCommonSettings.SectionName}:MaintenanceMode:Enabled"] = maintenanceEnabled.ToString(),
            [$"{WebHostCommonSettings.SectionName}:MaintenanceMode:RetryAt"] = RetryAt.ToString("O")
        });
        builder.Services
            .AddAspNetCommon(builder.Configuration.GetSection(WebHostCommonSettings.SectionName))
            .AddDialogportenHealthChecks(builder.Configuration, DialogportenHealthCheckExtensions.ProbeSectionName);

        await using var app = builder.Build();
        app.UsePathBase("/dialogporten");
        app.MapDialogportenHealthChecks();
        app.UseMaintenanceMode();
        app.MapGet(ApiPath, () => "ok");
        app.MapGet(AllowedPath, () => "ok").AllowDuringMaintenance();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        await assert(client);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
