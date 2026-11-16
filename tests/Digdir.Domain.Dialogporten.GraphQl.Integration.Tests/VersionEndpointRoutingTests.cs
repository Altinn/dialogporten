using System.Net;
using Digdir.Library.Utils.AspNet;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.GraphQl.Integration.Tests;

public class VersionEndpointRoutingTests
{
    private const string Version = "1.126.2-81787c8";

    // The edge forwards only /graphql to the GraphQL app, so its version endpoint shares a path prefix with the
    // catch-all route of the GraphQL endpoint and has to win over it.
    [Fact]
    public async Task Should_Take_Precedence_Over_GraphQL_Endpoint()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[VersionEndpointExtensions.VersionVariable] = Version;
        builder.Services
            .AddGraphQLServer()
            .AddQueryType(descriptor => descriptor.Field("ping").Resolve("pong"));

        await using var app = builder.Build();
        app.MapGraphQL();
        app.MapVersionEndpoint("/graphql/version");
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        using var versionResponse = await client.GetAsync(
            new Uri("/graphql/version", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var queryResponse = await client.GetAsync(
            new Uri($"/graphql?query={Uri.EscapeDataString("{ ping }")}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, versionResponse.StatusCode);
        Assert.Equal(Version, await versionResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        // The GraphQL endpoint still answers on its own path, so the version route did not win by default.
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        Assert.Contains("pong", await queryResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
