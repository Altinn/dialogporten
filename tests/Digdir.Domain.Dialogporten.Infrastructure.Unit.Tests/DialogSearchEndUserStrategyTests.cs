using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Selection;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogSearchEndUserStrategyTests
{
    [Fact]
    public void Selector_Should_Select_SinglePartyNoFts_For_SingleParty_Without_Fts() =>
        AssertSelectedStrategy<SinglePartyNoFtsStrategy>(
            CreateContext(partyCount: 1, hasFreeTextSearch: false));

    [Fact]
    public void Selector_Should_Select_SinglePartyFts_For_SingleParty_With_Fts() =>
        AssertSelectedStrategy<SinglePartyFtsStrategy>(
            CreateContext(partyCount: 1, hasFreeTextSearch: true));

    [Fact]
    public void Selector_Should_Select_DialogFirstFts_For_SmallMultiParty_With_Fts() =>
        AssertSelectedStrategy<DialogFirstFtsStrategy>(
            CreateContext(partyCount: 2, hasFreeTextSearch: true));

    [Fact]
    public void Selector_Should_Select_GenericPartyDriven_For_SmallMultiParty_Without_Fts() =>
        AssertSelectedStrategy<GenericPartyDrivenStrategy>(
            CreateContext(partyCount: 2, hasFreeTextSearch: false));

    [Fact]
    public void Selector_Should_Select_GinFirstFts_For_LargeMultiParty_With_Fts() =>
        AssertSelectedStrategy<GinFirstFtsStrategy>(
            CreateContext(partyCount: 1000, hasFreeTextSearch: true));

    [Fact]
    public void Selector_Should_Select_GenericServiceDriven_For_LargeMultiParty_Without_Fts_When_Effective_Service_Set_Is_Small() =>
        AssertSelectedStrategy<GenericServiceDrivenStrategy>(
            CreateContext(partyCount: 1000, hasFreeTextSearch: false));

    [Fact]
    public void Selector_Should_Select_GenericPartyDriven_For_LargeMultiParty_Without_Fts_When_Effective_Service_Set_Is_Large() =>
        AssertSelectedStrategy<GenericPartyDrivenStrategy>(
            CreateContext(partyCount: 1000, hasFreeTextSearch: false, serviceCount: 21));

    [Fact]
    public void Selector_Should_Select_SinglePartyNoFts_For_SingleParty_Without_Fts_With_Delegated_Dialogs() =>
        AssertSelectedStrategy<SinglePartyNoFtsStrategy>(
            CreateContext(
                partyCount: 1,
                hasFreeTextSearch: false,
                delegatedDialogIds: [Guid.CreateVersion7()]));

    [Fact]
    public void Selector_Should_Select_SinglePartyFts_For_SingleParty_With_Fts_With_Delegated_Dialogs() =>
        AssertSelectedStrategy<SinglePartyFtsStrategy>(
            CreateContext(
                partyCount: 1,
                hasFreeTextSearch: true,
                delegatedDialogIds: [Guid.CreateVersion7()]));

    private static void AssertSelectedStrategy<TStrategy>(EndUserSearchContext context)
        where TStrategy : IQueryStrategy<EndUserSearchContext>
    {
        var selector = CreateSelector(CreateSettings());

        var strategy = selector.Select(context);

        Assert.IsType<TStrategy>(strategy);
    }

    private static DialogEndUserSearchStrategySelector CreateSelector(ApplicationSettings settings)
    {
        var options = new TestOptionsSnapshot<ApplicationSettings>(settings);

        return new DialogEndUserSearchStrategySelector(
            [
                new SinglePartyFtsStrategy(
                    options,
                    NullLogger<SinglePartyFtsStrategy>.Instance),
                new DialogFirstFtsStrategy(
                    options,
                    NullLogger<DialogFirstFtsStrategy>.Instance),
                new GinFirstFtsStrategy(
                    options,
                    NullLogger<GinFirstFtsStrategy>.Instance),
                new SinglePartyNoFtsStrategy(),
                new GenericPartyDrivenStrategy(
                    options,
                    NullLogger<GenericPartyDrivenStrategy>.Instance),
                new GenericServiceDrivenStrategy(
                    options,
                    NullLogger<GenericServiceDrivenStrategy>.Instance)
            ],
            NullLogger<DialogEndUserSearchStrategySelector>.Instance);
    }

    private static EndUserSearchContext CreateContext(
        int partyCount,
        bool hasFreeTextSearch,
        int serviceCount = 1,
        Guid[]? delegatedDialogIds = null)
    {
        var services = Enumerable.Range(0, serviceCount)
            .Select(x => $"urn:altinn:resource:test-{x}")
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(
            "contentUpdatedAt_desc",
            out var orderSet));

        return new EndUserSearchContext(
            Query: new GetDialogsQuery
            {
                Deleted = false,
                OrderBy = orderSet,
                Limit = 100,
                Search = hasFreeTextSearch ? "melding" : null
            },
            AuthorizedResources: new DialogSearchAuthorizationResult
            {
                DialogIds = delegatedDialogIds?.ToList() ?? [],
                ResourcesByParties = Enumerable.Range(0, partyCount)
                    .ToDictionary(
                        x => $"urn:altinn:organization:identifier-no:{x:D9}",
                        _ => services,
                        StringComparer.Ordinal)
            });
    }

    private static ApplicationSettings CreateSettings() =>
        new()
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://localhost"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = CreateKeyPair(),
                    Secondary = CreateKeyPair()
                }
            },
            Limits = new LimitsSettings
            {
                EndUserSearch = new EndUserSearchQueryLimits
                {
                    MinServiceDrivenStrategyPartyCount = 100,
                    MaxFreeTextSearchCandidates = 5000,
                    MinFreeTextSearchCandidatesPerParty = 100,
                    MaxDialogFirstFreeTextSearchPartyCount = 50
                }
            }
        };

    private static Ed25519KeyPair CreateKeyPair() =>
        new()
        {
            Kid = "kid",
            PrivateComponent = "private",
            PublicComponent = "public"
        };

    private sealed class TestOptionsSnapshot<T>(T value) : IOptionsSnapshot<T>
        where T : class
    {
        public T Value => value;
        public T Get(string? name) => value;
    }
}
