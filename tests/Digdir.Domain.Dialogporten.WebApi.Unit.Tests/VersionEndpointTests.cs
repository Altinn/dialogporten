using System.Net;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class VersionEndpointTests
{
    private const string Version = "1.126.2-81787c8";
    private const string VersionPath = "/version";
    private const string ProtectedPath = "/protected";

    [Fact]
    public Task Should_Return_Version_As_Plain_Text() =>
        WithApp(Version, async client =>
        {
            using var response = await GetAsync(client, VersionPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Version, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        });

    // A cache that held an answer could tell a version check that the old version still runs, or that the new one
    // already does.
    [Fact]
    public Task Should_Mark_Response_No_Store() =>
        WithApp(Version, async client =>
        {
            using var response = await GetAsync(client, VersionPath);

            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        });

    [Fact]
    public Task Should_Return_Not_Found_When_Version_Is_Unset() =>
        WithApp(string.Empty, async client =>
        {
            using var response = await GetAsync(client, VersionPath);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });

    // A version check sends no token. The endpoint allows anonymous requests itself, so it stays open in an app
    // that requires a token for every other endpoint.
    [Fact]
    public Task Should_Allow_Anonymous_Requests_When_Other_Endpoints_Require_A_Token() =>
        WithApp(Version, async client =>
        {
            using var versionResponse = await GetAsync(client, VersionPath);
            using var protectedResponse = await GetAsync(client, ProtectedPath);

            Assert.Equal(HttpStatusCode.OK, versionResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        });

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string path) =>
        client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

    private static async Task WithApp(string version, Func<HttpClient, Task> assert)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[VersionEndpointExtensions.VersionVariable] = version;
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapVersionEndpoint(VersionPath);
        app.MapGet(ProtectedPath, () => Results.Ok());
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        await assert(client);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
