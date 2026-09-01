using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Metadata;

[Collection(nameof(WebApiTestCollectionFixture))]
public class ServiceResourcesMetadataTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private readonly WebApiE2EFixture _fixture = fixture;

    [E2EFact]
    public async Task Should_Return_Uncompressed_Response_By_Default()
    {
        // Act
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.MetadataApi.V1MetadataServiceResourcesGetServiceResourceMetadata(languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.ContentHeaders!.ContentEncoding.Should().BeEmpty();
    }

    [E2ETheory]
    [InlineData("gzip")]
    [InlineData("br")]
    public async Task Should_Return_Compressed_Response(string acceptEncoding)
    {
        // Act
        using var client = Fixture.HttpClientFactory.CreateClient();
        client.BaseAddress = new UriBuilder(_fixture.Settings.DialogportenBaseUri)
        {
            Port = _fixture.Settings.WebAPiPort
        }.Uri;
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/metadata/serviceresources");
        request.Headers.Add("Accept-Encoding", acceptEncoding);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Single().Should().Be(acceptEncoding);
    }
}
