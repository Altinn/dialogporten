using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Swagger;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SwaggerTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private readonly WebApiE2EFixture _fixture = fixture;

    [E2EFact]
    public async Task Should_Redirect_Successfully_From_Swagger_Directory()
    {
        using var handler = new SocketsHttpHandler();
        handler.AllowAutoRedirect = false;
        using var client = new HttpClient(handler);
        client.BaseAddress = _fixture.WebApiUri;

        var response = await client.GetAsync("swagger");
        var requestPath = response.RequestMessage!.RequestUri!.AbsolutePath ?? throw new UnreachableException();

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().Be(requestPath + "/index.html");
    }

    [E2ETheory]
    [InlineData("v1")]
    [InlineData("v1.enduser")]
    [InlineData("v1.serviceowner")]
    public async Task Should_Host_All_OpenAPI_Specs(string specification)
    {
        using var client = _fixture.GetHttpClientFactory().CreateClient();
        client.BaseAddress = _fixture.WebApiUri;

        var response = await client.GetAsync($"swagger/{specification}/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = response.Content.ReadAsStringAsync();
        body.Result.Should().NotBeNull();
    }
}
