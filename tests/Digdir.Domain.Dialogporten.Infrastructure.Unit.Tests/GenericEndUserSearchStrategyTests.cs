using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Selection;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class GenericEndUserSearchStrategyTests
{
    [Fact]
    public void ServiceDrivenScore_ShouldBePreferred_WhenMoreThanOneHundredEffectivePartiesAndServiceFilterWithoutSearch()
    {
        var strategy = CreateServiceDrivenStrategy();
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Preferred, score);
    }

    [Fact]
    public void ServiceDrivenScore_ShouldBeEligible_WhenSearchIsPresent()
    {
        var strategy = CreateServiceDrivenStrategy();
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            search: "invoice",
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Eligible, score);
    }

    [Fact]
    public void ServiceDrivenScore_ShouldUseEffectivePartyCountAfterServiceFilter()
    {
        var strategy = CreateServiceDrivenStrategy();
        var authorizations = CreateAuthorizations(101, "resource2");
        authorizations["party1"] = ["resource1"];
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            authorizedResourcesByParties: authorizations);

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Eligible, score);
    }

    [Fact]
    public void ServiceDrivenScore_ShouldUseConfiguredPartyThreshold()
    {
        var strategy = CreateServiceDrivenStrategy(minServiceDrivenStrategyPartyCount: 10);
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            authorizedResourcesByParties: CreateAuthorizations(11, "resource1"));

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Preferred, score);
    }

    [Fact]
    public void PartyDrivenScore_ShouldBePreferred_WhenSearchIsPresent()
    {
        var strategy = CreatePartyDrivenStrategy();
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            search: "invoice",
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Preferred, score);
    }

    [Fact]
    public void PartyDrivenScore_ShouldBePreferred_WhenNoServiceFilterIsPresent()
    {
        var strategy = CreatePartyDrivenStrategy();
        var context = CreateContext(
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Preferred, score);
    }

    [Fact]
    public void Selector_ShouldChooseServiceDrivenStrategy_ForManyPartiesWithServiceFilterAndNoSearch()
    {
        var partyStrategy = CreatePartyDrivenStrategy();
        var serviceStrategy = CreateServiceDrivenStrategy();
        var selector = new DialogEndUserSearchStrategySelector(
            [partyStrategy, serviceStrategy],
            NullLogger<DialogEndUserSearchStrategySelector>.Instance);
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var selected = selector.Select(context);

        Assert.Equal(serviceStrategy.Name, selected.Name);
    }

    [Fact]
    public void Selector_ShouldChoosePartyDrivenStrategy_ForSearch()
    {
        var partyStrategy = CreatePartyDrivenStrategy();
        var serviceStrategy = CreateServiceDrivenStrategy();
        var selector = new DialogEndUserSearchStrategySelector(
            [serviceStrategy, partyStrategy],
            NullLogger<DialogEndUserSearchStrategySelector>.Instance);
        var context = CreateContext(
            queryServiceResources: ["resource1"],
            search: "invoice",
            authorizedResourcesByParties: CreateAuthorizations(101, "resource1"));

        var selected = selector.Select(context);

        Assert.Equal(partyStrategy.Name, selected.Name);
    }

    [Fact]
    public void PartyDrivenBuildSql_ShouldUsePartyPermissions()
    {
        var strategy = CreatePartyDrivenStrategy();
        var context = CreateContext(authorizedResourcesByParties: CreateAuthorizations(2, "resource1"));

        var (sql, _) = strategy.BuildSql(context).ToDynamicParameters();

        Assert.Contains("party_permissions AS", sql, StringComparison.Ordinal);
        Assert.Contains("FROM party_permissions pp", sql, StringComparison.Ordinal);
        Assert.Contains("d.\"Party\" = pp.party", sql, StringComparison.Ordinal);
        Assert.Contains("d.\"ServiceResource\" = ANY(pp.allowed_services)", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("service_permissions AS", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void PartyDrivenBuildSql_ShouldConstrainSearchByParty()
    {
        var strategy = CreatePartyDrivenStrategy();
        var context = CreateContext(
            search: "invoice",
            authorizedResourcesByParties: CreateAuthorizations(2, "resource1"));

        var (sql, _) = strategy.BuildSql(context).ToDynamicParameters();

        Assert.Contains("JOIN search.\"DialogSearch\" ds ON d.\"Id\" = ds.\"DialogId\"", sql, StringComparison.Ordinal);
        Assert.Contains("ds.\"Party\" = pp.party", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceDrivenBuildSql_ShouldUseServicePermissions()
    {
        var strategy = CreateServiceDrivenStrategy();
        var context = CreateContext(authorizedResourcesByParties: CreateAuthorizations(2, "resource1"));

        var (sql, _) = strategy.BuildSql(context).ToDynamicParameters();

        Assert.Contains("service_permissions AS", sql, StringComparison.Ordinal);
        Assert.Contains("FROM service_permissions sp", sql, StringComparison.Ordinal);
        Assert.Contains("d.\"ServiceResource\" = sp.service", sql, StringComparison.Ordinal);
        Assert.Contains("d.\"Party\" = ANY(sp.allowed_parties)", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("party_permissions AS", sql, StringComparison.Ordinal);
    }

    private static EndUserSearchContext CreateContext(
        List<string>? queryParties = null,
        List<string>? queryServiceResources = null,
        string? search = null,
        Dictionary<string, HashSet<string>>? authorizedResourcesByParties = null)
    {
        var query = new GetDialogsQuery
        {
            Deleted = false,
            Limit = 10,
            OrderBy = OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.Default,
            Party = queryParties,
            ServiceResource = queryServiceResources,
            Search = search
        };

        var authorizationResult = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = authorizedResourcesByParties ?? new Dictionary<string, HashSet<string>>()
        };

        return new EndUserSearchContext(query, authorizationResult);
    }

    private static Dictionary<string, HashSet<string>> CreateAuthorizations(int partyCount, params string[] services) =>
        Enumerable
            .Range(1, partyCount)
            .ToDictionary(
                partyNumber => $"party{partyNumber}",
                _ => services.ToHashSet(StringComparer.Ordinal));

    private static GenericServiceDrivenStrategy CreateServiceDrivenStrategy(
        int minServiceDrivenStrategyPartyCount = 100) =>
        new(
            CreateApplicationSettings(minServiceDrivenStrategyPartyCount),
            NullLogger<GenericServiceDrivenStrategy>.Instance);

    private static GenericPartyDrivenStrategy CreatePartyDrivenStrategy(
        int minServiceDrivenStrategyPartyCount = 100) =>
        new(
            CreateApplicationSettings(minServiceDrivenStrategyPartyCount),
            NullLogger<GenericPartyDrivenStrategy>.Instance);

    private static TestOptionsSnapshot<ApplicationSettings> CreateApplicationSettings(
        int minServiceDrivenStrategyPartyCount) =>
        new(new ApplicationSettings
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://example.com"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = new Ed25519KeyPair
                    {
                        Kid = "primary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    },
                    Secondary = new Ed25519KeyPair
                    {
                        Kid = "secondary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    }
                }
            },
            Limits = new LimitsSettings
            {
                EndUserSearch = new EndUserSearchQueryLimits
                {
                    MinServiceDrivenStrategyPartyCount = minServiceDrivenStrategyPartyCount
                }
            }
        });

    private sealed class TestOptionsSnapshot<T>(T value) : IOptionsSnapshot<T>
        where T : class
    {
        public T Value { get; } = value;

        public T Get(string? name) => Value;
    }
}
