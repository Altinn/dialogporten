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

    // A deploy asks for this header to see when the new image tag answers. Through the edge, some apps can only be
    // reached on paths that reject the request, so challenge, not-found and handled error responses must carry it too.
    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/protected", HttpStatusCode.Unauthorized)]
    [InlineData("/missing", HttpStatusCode.NotFound)]
    [InlineData("/throws", HttpStatusCode.InternalServerError)]
    public Task Should_Send_Version_Header_When_Requested(string path, HttpStatusCode expectedStatus) =>
        WithApp(Version, async client =>
        {
            using var response = await GetAsync(client, path, requestVersion: true);

            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(VersionHeaderExtensions.HeaderName, out var values));
            Assert.Equal(Version, Assert.Single(values));
        });

    [Theory]
    [InlineData("/health")]
    [InlineData("/protected")]
    public Task Should_Not_Send_Version_Header_When_Not_Requested(string path) =>
        WithApp(Version, async client =>
        {
            using var response = await GetAsync(client, path, requestVersion: false);

            Assert.False(response.Headers.Contains(VersionHeaderExtensions.HeaderName));
        });

    [Fact]
    public Task Should_Not_Send_Version_Header_When_Version_Is_Unset() =>
        WithApp(string.Empty, async client =>
        {
            using var response = await GetAsync(client, "/health", requestVersion: true);

            Assert.False(response.Headers.Contains(VersionHeaderExtensions.HeaderName));
        });

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, bool requestVersion)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        if (requestVersion)
        {
            request.Headers.Add(VersionHeaderExtensions.RequestHeaderName, "true");
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

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
