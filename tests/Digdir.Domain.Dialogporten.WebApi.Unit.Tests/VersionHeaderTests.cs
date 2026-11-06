using System.Net;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class VersionHeaderTests
{
    private const string Version = "1.126.2-81787c8";

    // A deploy waits for this header to show the new image tag. Through the edge, some apps can only be reached
    // on paths that reject the request, so challenge, not-found and handled error responses must carry it too.
    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/protected", HttpStatusCode.Unauthorized)]
    [InlineData("/missing", HttpStatusCode.NotFound)]
    [InlineData("/throws", HttpStatusCode.InternalServerError)]
    public Task Should_Send_Version_Header_On_Every_Response(string path, HttpStatusCode expectedStatus) =>
        WithApp(Version, async client =>
        {
            using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(VersionHeaderExtensions.HeaderName, out var values));
            Assert.Equal(Version, Assert.Single(values));
        });

    [Fact]
    public Task Should_Not_Send_Version_Header_When_Version_Is_Unset() =>
        WithApp(string.Empty, async client =>
        {
            using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

            Assert.False(response.Headers.Contains(VersionHeaderExtensions.HeaderName));
        });

    private static async Task WithApp(string version, Func<HttpClient, Task> assert)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[VersionHeaderExtensions.VersionVariable] = version;
        builder.Services.AddDialogportenHealthChecks(
            builder.Configuration,
            DialogportenHealthCheckExtensions.ProbeSectionName);

        await using var app = builder.Build();
        app.UseVersionHeader();
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
        app.MapDialogportenHealthChecks();
        app.MapGet("/protected", () => Results.Unauthorized());
        app.Map("/throws", _ => throw new InvalidOperationException("Handled by the exception handler."));
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        await assert(client);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
