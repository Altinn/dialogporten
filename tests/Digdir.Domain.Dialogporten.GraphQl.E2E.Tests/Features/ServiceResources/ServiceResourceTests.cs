using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

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
    public async Task Should_Return_Data_For_Search_Service_Resources()
    {
        // Arrange
        await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var result = await Fixture.GraphQlClient.SearchServiceResources.ExecuteAsync(null);

        // Assert
        result.Data.Should().NotBeNull();
    }

    [E2EFact]
    public async Task Should_Return_Data_For_Search_Service_Resources_With_Party_Filter()
    {
        // Arrange
        await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        // Act
        var result = await Fixture.GraphQlClient.SearchServiceResources.ExecuteAsync([E2EConstants.DefaultParty]);

        // Assert
        result.Data.Should().NotBeNull();
    }
}
