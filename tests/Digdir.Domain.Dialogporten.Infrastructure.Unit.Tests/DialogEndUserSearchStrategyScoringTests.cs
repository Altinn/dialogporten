using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class DialogEndUserSearchStrategyScoringTests
{
    [Fact]
    public void Score_ShouldPreferPartyDriven_WhenServiceAndPartyCardinalityAreLow()
    {
        // Arrange
        var context = CreateContext(BuildResourcesByParties(partyCount: 3, serviceCount: 2));
        var partyDriven = new PartyDrivenDialogEndUserSearchStrategy(NullLogger<PartyDrivenDialogEndUserSearchStrategy>.Instance);
        var serviceDriven = new ServiceDrivenDialogEndUserSearchStrategy(NullLogger<ServiceDrivenDialogEndUserSearchStrategy>.Instance);

        // Act
        var partyScore = partyDriven.Score(context);
        var serviceScore = serviceDriven.Score(context);

        // Assert
        Assert.Equal(0, partyScore);
        Assert.Equal(100, serviceScore);
    }

    [Fact]
    public void Score_ShouldPreferServiceDriven_WhenServiceCardinalityIsHigh()
    {
        // Arrange
        var context = CreateContext(BuildResourcesByParties(partyCount: 3, serviceCount: 6));
        var partyDriven = new PartyDrivenDialogEndUserSearchStrategy(NullLogger<PartyDrivenDialogEndUserSearchStrategy>.Instance);
        var serviceDriven = new ServiceDrivenDialogEndUserSearchStrategy(NullLogger<ServiceDrivenDialogEndUserSearchStrategy>.Instance);

        // Act
        var partyScore = partyDriven.Score(context);
        var serviceScore = serviceDriven.Score(context);

        // Assert
        Assert.Equal(0, partyScore);
        Assert.Equal(100, serviceScore);
    }

    [Fact]
    public void Score_ShouldPreferPartyDriven_WhenPartyCardinalityIsVeryHigh_AndServiceCardinalityIsLow()
    {
        // Arrange
        var context = CreateContext(BuildResourcesByParties(partyCount: 5000, serviceCount: 1));
        var partyDriven = new PartyDrivenDialogEndUserSearchStrategy(NullLogger<PartyDrivenDialogEndUserSearchStrategy>.Instance);
        var serviceDriven = new ServiceDrivenDialogEndUserSearchStrategy(NullLogger<ServiceDrivenDialogEndUserSearchStrategy>.Instance);

        // Act
        var partyScore = partyDriven.Score(context);
        var serviceScore = serviceDriven.Score(context);

        // Assert
        Assert.Equal(0, partyScore);
        Assert.Equal(100, serviceScore);
    }

    [Fact]
    public void Score_ShouldPreferServiceDriven_WhenServiceCardinalityIsHigh_EvenWhenPartyCardinalityIsVeryHigh()
    {
        // Arrange
        var context = CreateContext(
            BuildResourcesByParties(partyCount: 5000, serviceCount: 6));
        var partyDriven = new PartyDrivenDialogEndUserSearchStrategy(NullLogger<PartyDrivenDialogEndUserSearchStrategy>.Instance);
        var serviceDriven = new ServiceDrivenDialogEndUserSearchStrategy(NullLogger<ServiceDrivenDialogEndUserSearchStrategy>.Instance);

        // Act
        var partyScore = partyDriven.Score(context);
        var serviceScore = serviceDriven.Score(context);

        // Assert
        Assert.Equal(0, partyScore);
        Assert.Equal(100, serviceScore);
    }

    private static EndUserSearchContext CreateContext(
        Dictionary<string, HashSet<string>> resourcesByParties,
        List<string>? constrainedParties = null,
        List<string>? constrainedServices = null)
    {
        var query = new GetDialogsQuery
        {
            Deleted = false,
            Party = constrainedParties,
            ServiceResource = constrainedServices
        };

        var authorizationResult = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = resourcesByParties
        };

        return new EndUserSearchContext(query, authorizationResult);
    }

    private static Dictionary<string, HashSet<string>> BuildResourcesByParties(int partyCount, int serviceCount) =>
        Enumerable.Range(1, partyCount)
            .ToDictionary(
                i => $"party-{i}",
                _ => Enumerable.Range(1, serviceCount)
                    .Select(s => $"service-{s}")
                    .ToHashSet(StringComparer.InvariantCultureIgnoreCase));
}
