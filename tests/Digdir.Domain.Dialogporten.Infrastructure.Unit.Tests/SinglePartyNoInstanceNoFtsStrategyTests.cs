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

public class SinglePartyNoInstanceNoFtsStrategyTests
{
    [Fact]
    public void Score_ShouldBeHighlyPreferred_WhenEffectiveAuthorizationContainsOnePartyWithoutSearchOrDelegations()
    {
        var strategy = new SinglePartyNoInstanceNoFtsStrategy();
        var context = CreateContext(
            queryParties: ["party1"],
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1", "resource2"],
                ["party2"] = ["resource3"]
            });

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.HighlyPreferred, score);
    }

    [Fact]
    public void Score_ShouldBeIneligible_WhenEffectiveAuthorizationContainsMultipleParties()
    {
        var strategy = new SinglePartyNoInstanceNoFtsStrategy();
        var context = CreateContext(
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1"],
                ["party2"] = ["resource2"]
            });

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Ineligible, score);
    }

    [Fact]
    public void Score_ShouldBeIneligible_WhenSearchIsPresent()
    {
        var strategy = new SinglePartyNoInstanceNoFtsStrategy();
        var context = CreateContext(
            queryParties: ["party1"],
            search: "invoice",
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1"]
            });

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Ineligible, score);
    }

    [Fact]
    public void Score_ShouldBeIneligible_WhenDelegatedDialogIdsArePresent()
    {
        var strategy = new SinglePartyNoInstanceNoFtsStrategy();
        var context = CreateContext(
            queryParties: ["party1"],
            delegatedDialogIds: [Guid.CreateVersion7()],
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1"]
            });

        var score = strategy.Score(context);

        Assert.Equal(QueryStrategyScores.Ineligible, score);
    }

    [Fact]
    public void Selector_ShouldChooseSinglePartyNoInstanceNoFtsStrategy_ForSinglePartyContextWithoutSearchOrDelegations()
    {
        var singlePartyStrategy = new SinglePartyNoInstanceNoFtsStrategy();
        var applicationSettings = CreateApplicationSettings();
        var partyStrategy = new GenericPartyDrivenStrategy(
            applicationSettings,
            NullLogger<GenericPartyDrivenStrategy>.Instance);
        var serviceStrategy = new GenericServiceDrivenStrategy(
            applicationSettings,
            NullLogger<GenericServiceDrivenStrategy>.Instance);
        var selector = new DialogEndUserSearchStrategySelector(
            [partyStrategy, serviceStrategy, singlePartyStrategy],
            NullLogger<DialogEndUserSearchStrategySelector>.Instance);
        var context = CreateContext(
            queryParties: ["party1"],
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1", "resource2"],
                ["party2"] = ["resource3"]
            });

        var selected = selector.Select(context);

        Assert.Equal(singlePartyStrategy.Name, selected.Name);
    }

    [Fact]
    public void BuildSql_ShouldCreateDirectDialogQuery_ForSinglePartyQueryWithoutSearchOrDelegations()
    {
        var strategy = new SinglePartyNoInstanceNoFtsStrategy();
        var context = CreateContext(
            queryParties: ["party1"],
            queryServiceResources: ["resource1"],
            authorizedResourcesByParties: new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1", "resource2"]
            });

        var (sql, _) = strategy.BuildSql(context).ToDynamicParameters();

        Assert.Contains("SELECT d.*", sql, StringComparison.Ordinal);
        Assert.Contains("FROM \"Dialog\" d", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("WITH", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CROSS JOIN LATERAL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delegated_dialogs", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("candidate_dialogs", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("search.\"DialogSearch\"", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static EndUserSearchContext CreateContext(
        List<string>? queryParties = null,
        List<string>? queryServiceResources = null,
        string? search = null,
        List<Guid>? delegatedDialogIds = null,
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
            DialogIds = delegatedDialogIds ?? [],
            ResourcesByParties = authorizedResourcesByParties ?? new Dictionary<string, HashSet<string>>()
        };

        return new EndUserSearchContext(query, authorizationResult);
    }

    private static TestOptionsSnapshot<ApplicationSettings> CreateApplicationSettings() =>
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
            }
        });

    private sealed class TestOptionsSnapshot<T>(T value) : IOptionsSnapshot<T>
        where T : class
    {
        public T Value { get; } = value;

        public T Get(string? name) => Value;
    }
}
