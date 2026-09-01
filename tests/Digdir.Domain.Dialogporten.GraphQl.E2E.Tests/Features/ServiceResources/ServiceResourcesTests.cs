using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.ServiceResources;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class ServiceResourceTests(GraphQlE2EFixture fixture) : E2ETestBase<GraphQlE2EFixture>(fixture)
{

    [E2EFact]
    public async Task Should_Return_Data_For_Get_Service_Resources()
    {
        // Arrange
        // Act
        var result = await Fixture.GraphQlClient.GetServiceResources.ExecuteAsync();

        // Assert
        result.Data.Should().NotBeNull();
    }

    [E2EFact]
    public async Task Should_Return_Uncompressed_Response_By_Default()
    {
        // Act
        using var client = Fixture.HttpClientFactory.CreateClient("DialogportenGraphQlTestClient");
        var request = CreateGetServiceResourcesRequest();
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Should().BeEmpty();
    }

    [E2ETheory]
    [InlineData("gzip")]
    [InlineData("br")]
    public async Task Should_Return_Compressed_Response(string acceptEncoding)
    {
        // Act
        using var client = Fixture.HttpClientFactory.CreateClient("DialogportenGraphQlTestClient");
        var request = CreateGetServiceResourcesRequest();
        request.Headers.Add("Accept-Encoding", acceptEncoding);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Single().Should().Be(acceptEncoding);
    }

    private static HttpRequestMessage CreateGetServiceResourcesRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(new
            {
                query = """
                    query GetServiceResources {                                                                                                                                                                                         
                      serviceResources {                                                                                                                                                                                                
                        items {                                                                                                                                                                                                         
                          serviceResource { id resourceType status }                                                                                                                                                                    
                        }                                                                                                                                                                                                               
                      }                                                                                                                                                                                                                 
                    }      
                    """
            })
        };
        return request;
    }
}
