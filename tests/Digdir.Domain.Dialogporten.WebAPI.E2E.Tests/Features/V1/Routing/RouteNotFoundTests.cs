using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Routing;

[Collection(nameof(WebApiTestCollectionFixture))]
public class RouteNotFoundTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private readonly WebApiE2EFixture _fixture = fixture;

    [E2EFact]
    public async Task Should_Return_404_With_Expected_Body()
    {
        using var client = _fixture.GetHttpClientFactory().CreateClient();
        client.BaseAddress = new UriBuilder(_fixture.Settings.DialogportenBaseUri)
        {
            Port = _fixture.Settings.WebAPiPort
        }.Uri;

        var response = await client.GetAsync("unknown-endpoint");
        var requestPath = response.RequestMessage!.RequestUri!.AbsolutePath ?? throw new UnreachableException();

        response.Content.Should().NotBeNull();
        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(json, JsonSerializerOptions.Web);

        problemDetails.Should().NotBeNull();
        problemDetails.Code.Should().BeNull();
        problemDetails.Detail.Should().BeNull();
        problemDetails.Errors.Should().BeNull();
        problemDetails.Instance.Should().Be(requestPath);
        problemDetails.Status.Should().Be((int)HttpStatusCode.NotFound);
        problemDetails.StatusDescription.Should().BeNull();
        problemDetails.Title.Should().Be("Endpoint not found.");
        problemDetails.TraceId.Should().NotBeNull();
        problemDetails.ValidationErrors.Should().BeNull();
    }
}
