using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Models;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogEndUserSearchSqlHelpersTests
{
    [Fact]
    public void BuildPartiesAndServices_ShouldGroupAlreadyEffectiveAuthorizationsByIdenticalServiceSets()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a", "resource-b"]),
            ("party2", ["resource-b", "resource-a"]),
            ("party3", ["resource-a", "resource-c"]),
            ("party4", ["resource-d"]));

        var groups = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(authorization);

        AssertGroups(
            groups,
            (["party1", "party2"], ["resource-a", "resource-b"]),
            (["party3"], ["resource-a", "resource-c"]),
            (["party4"], ["resource-d"]));
        AssertAuthorizedPairsExactly(authorization, groups);
    }

    [Fact]
    public void BuildPartiesAndServices_ShouldIgnoreEmptyServiceSets()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a"]),
            ("party2", []));

        var groups = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(authorization);

        AssertGroups(groups, (["party1"], ["resource-a"]));
        AssertAuthorizedPairsExactly(authorization, groups);
    }

    [Fact]
    public void CountEffectiveParties_ShouldCountAuthorizedPartiesWithEffectiveResources()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a"]),
            ("party2", ["resource-b"]),
            ("party3", []));

        var count = DialogEndUserSearchSqlHelpers.CountEffectiveParties(authorization);

        Assert.Equal(2, count);
    }

    [Fact]
    public void CountEffectiveServices_ShouldCountDistinctServicesAcrossEffectiveAuthorizations()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a", "resource-b"]),
            ("party2", ["resource-a", "resource-c"]),
            ("party3", ["resource-d"]));

        var count = DialogEndUserSearchSqlHelpers.CountEffectiveServices(authorization);

        Assert.Equal(4, count);
    }

    [Fact]
    public void TryGetSinglePartyAuthorization_ShouldReturnSingleEffectiveParty()
    {
        var authorization = CreateAuthorization(("party1", ["resource-a", "resource-b"]));

        var found = DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            authorization,
            out var singlePartyAuthorization);

        Assert.True(found);
        Assert.NotNull(singlePartyAuthorization);
        Assert.Equal("party1", singlePartyAuthorization.Party);
        Assert.Equal(
            ["resource-a", "resource-b"],
            singlePartyAuthorization.Services.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void TryGetSinglePartyAuthorization_ShouldReturnSingleEffectiveParty_WhenOtherPartiesHaveEmptyServiceSets()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a", "resource-b"]),
            ("party2", []));

        var found = DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            authorization,
            out var singlePartyAuthorization);

        Assert.True(found);
        Assert.NotNull(singlePartyAuthorization);
        Assert.Equal("party1", singlePartyAuthorization.Party);
        Assert.Equal(
            ["resource-a", "resource-b"],
            singlePartyAuthorization.Services.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void TryGetSinglePartyAuthorization_ShouldReturnFalse_ForMultipleEffectiveParties()
    {
        var authorization = CreateAuthorization(
            ("party1", ["resource-a"]),
            ("party2", ["resource-a"]));

        var found = DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            authorization,
            out var singlePartyAuthorization);

        Assert.False(found);
        Assert.Null(singlePartyAuthorization);
    }

    [Fact]
    public void TryGetSinglePartyAuthorization_ShouldReturnFalse_ForNoEffectiveParties()
    {
        var authorization = new DialogSearchAuthorizationResult();

        var found = DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            authorization,
            out var singlePartyAuthorization);

        Assert.False(found);
        Assert.Null(singlePartyAuthorization);
    }

    private static DialogSearchAuthorizationResult CreateAuthorization(
        params (string Party, string[] Services)[] authorizations) =>
        new()
        {
            ResourcesByParties = authorizations.ToDictionary(
                x => x.Party,
                x => x.Services.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal)
        };

    private static void AssertGroups(
        List<PartiesAndServices> actualGroups,
        params (string[] Parties, string[] Services)[] expectedGroups)
    {
        var actual = actualGroups.ToDictionary(
            x => CreateServiceKey(x.Services),
            x => x.Parties.OrderBy(party => party, StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);

        Assert.Equal(expectedGroups.Length, actual.Count);
        foreach (var (parties, services) in expectedGroups)
        {
            var serviceKey = CreateServiceKey(services);
            Assert.True(actual.TryGetValue(serviceKey, out var actualParties), $"Missing service group: {serviceKey}");
            Assert.Equal(
                parties.OrderBy(party => party, StringComparer.Ordinal).ToArray(),
                actualParties);
        }
    }

    private static void AssertAuthorizedPairsExactly(
        DialogSearchAuthorizationResult authorization,
        List<PartiesAndServices> groups)
    {
        var expectedPairs = authorization.ResourcesByParties
            .SelectMany(x => x.Value.Select(service => CreatePairKey(x.Key, service)))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var actualPairs = groups
            .SelectMany(group => group.Parties.SelectMany(party =>
                group.Services.Select(service => CreatePairKey(party, service))))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedPairs, actualPairs);
    }

    private static string CreateServiceKey(IEnumerable<string> services) =>
        string.Join('|', services.OrderBy(service => service, StringComparer.Ordinal));

    private static string CreatePairKey(string party, string service) => $"{party}|{service}";
}
