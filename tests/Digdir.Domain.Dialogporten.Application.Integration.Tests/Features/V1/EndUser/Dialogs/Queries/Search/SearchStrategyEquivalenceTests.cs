using AwesomeAssertions;
using Bogus;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using DialogGenerator = Digdir.Tool.Dialogporten.GenerateFakeData.DialogGenerator;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class SearchStrategyEquivalenceTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string MatchTerm = "needle";
    private const string AlphaTerm = "alpha";
    private const string BravoTerm = "bravo";
    private const string DelegatedTerm = "delegate";
    private const string ServicePrefix = "urn:altinn:resource:strategy-equivalence";
    private static DateTimeOffset DateAnchor => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Search_WithoutFts_Should_ReturnExpectedDialogs_ForSingleEffectiveParty()
    {
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 4);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnExpectedDialogs_ForSingleEffectiveParty()
    {
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 4, matchingIndexes: [0, 2]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties, search: MatchTerm);

        AssertIds(result, ExpectedIds(dialogs, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithoutFts_Should_ReturnExpectedDialogs_ForFewEffectiveParties()
    {
        var parties = CreateParties(2);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 6);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnExpectedDialogs_ForFewEffectiveParties()
    {
        var parties = CreateParties(2);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 6, matchingIndexes: [1, 3, 5]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties, search: MatchTerm);

        AssertIds(result, ExpectedIds(dialogs, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnExpectedDialogs_ForManyEffectiveParties()
    {
        var parties = CreateParties(51);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 51, matchingIndexes: Enumerable.Range(0, 51));

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(services: services, search: MatchTerm, limit: 100);

        AssertIds(result, ExpectedIds(dialogs, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithoutFts_Should_ReturnExpectedDialogs_ForManyPartiesFewEffectiveServices()
    {
        var parties = CreateParties(101);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 101);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(services: services, limit: 200);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_WithoutFts_Should_ReturnExpectedDialogs_ForManyPartiesMultipleEffectiveServices()
    {
        // Many parties (> threshold) across 2..20 services, no FTS -> exercises MultiServiceStrategy's
        // per-service UNION (each block drives the service index and early-terminates; branches merge-appended).
        var parties = CreateParties(101);
        var services = CreateServices(2);
        var dialogs = CreateDialogs(parties, services, count: 202);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(services: services, limit: 300);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_WithoutFts_Should_ReturnExpectedDialogs_ForManyPartiesManyEffectiveServices()
    {
        var parties = CreateParties(101);
        var services = CreateServices(21);
        var dialogs = CreateDialogs(parties, services, count: 101);

        ConfigureApplicationSettings(maxPartyFilterValues: 200);
        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties, limit: 200);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnExpectedDialogs_ForManyPartiesSingleEffectiveService()
    {
        // #4128: no party filter, a single service resource, many authorized parties (> the service-driven
        // threshold of 100) -> exercises SingleServiceFtsStrategy (scalar service + bound party param).
        var parties = CreateParties(101);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(
            parties,
            services,
            count: 101,
            matchingIndexes: Enumerable.Range(0, 101).Where(x => x % 2 == 0));

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(services: services, search: MatchTerm, limit: 200);

        AssertIds(result, ExpectedIds(dialogs, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnExpectedDialogs_ForManyPartiesMultipleEffectiveServices()
    {
        // Many authorized parties (> the service-driven threshold) across more than one service ->
        // exercises MultiServiceFtsStrategy's per-service UNION (each service block drives the service
        // index and early-terminates; branches are merge-appended).
        var parties = CreateParties(101);
        var services = CreateServices(2);
        var dialogs = CreateDialogs(
            parties,
            services,
            count: 202,
            matchingIndexes: Enumerable.Range(0, 202).Where(x => x % 3 == 0));

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(services: services, search: MatchTerm, limit: 300);

        AssertIds(result, ExpectedIds(dialogs, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_AndContentUpdatedRange_Should_ReturnOnlyMatchesWithinRange()
    {
        // The temporal escape hatch: a ContentUpdatedAfter range bounds an FTS search.
        // ContentUpdatedAt == UpdatedAt == CreatedAt (== DateAnchor + index minutes) at creation time,
        // so the cutoff below includes only matches with index >= 3.
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 6, matchingIndexes: [0, 2, 4, 5]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var contentUpdatedAfter = DateAnchor.AddMinutes(2).AddSeconds(30);

        var result = await Search(parties: parties, search: MatchTerm, contentUpdatedAfter: contentUpdatedAfter);

        AssertIds(result, ExpectedIds(dialogs, x =>
            x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)
            && x.CreatedAt >= contentUpdatedAfter));
    }

    [Fact]
    public async Task Search_WithFts_Should_ExcludeMatches_ForUnauthorizedService()
    {
        // Authorization is applied on the Dialog join, not bypassed by the GIN term probe:
        // a matching dialog under a service the caller is not authorized for must be excluded.
        var parties = CreateParties(1);
        var services = CreateServices(2);
        var authorizedMatch = CreateDialog(parties[0], services[0], index: 0, searchText: $"{MatchTerm} authorized");
        var unauthorizedMatch = CreateDialog(parties[0], services[1], index: 1, searchText: $"{MatchTerm} unauthorized");
        var dialogs = new[] { authorizedMatch, unauthorizedMatch };

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [parties[0]] = new HashSet<string> { services[0] }
            }
        });

        var result = await Search(parties: parties, search: MatchTerm);

        AssertIds(result, [authorizedMatch.Id]);
    }

    [Fact]
    public async Task Search_WithFts_Should_ReturnDelegatedDialogs_WhenNoAuthorizedParties()
    {
        // Delegated-only authorization: the request carries a serviceResource filter (required by the
        // validator), but the caller is authorized for no party on it — authorization fans out to empty
        // ResourcesByParties + non-empty DialogIds. effectivePartyCount == 0, so MultiPartyFtsStrategy owns
        // the fallback and the delegated UNION branch carries the results (no other FTS strategy is eligible).
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var delegatedMatch = CreateDialog(parties[0], services[0], index: 0, searchText: DelegatedTerm);
        var unrelated = CreateDialog(parties[0], services[0], index: 1, searchText: $"ordinary {DelegatedTerm}x");
        var dialogs = new[] { delegatedMatch, unrelated };

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            DialogIds = [delegatedMatch.Id]
        });

        var result = await Search(services: services, search: DelegatedTerm);

        AssertIds(result, [delegatedMatch.Id]);
    }

    [Fact]
    public async Task Search_WithFts_Should_IncludeDelegatedDialogs_ForManyPartiesMultipleEffectiveServices()
    {
        // Exercises the delegated-UNION branch of the multi-service strategy: a delegated dialog owned by
        // an UNauthorized party must still be returned alongside the directly-authorized matches.
        var allParties = CreateParties(102);
        var authorizedParties = allParties[..101];
        var services = CreateServices(2);
        var dialogs = CreateDialogs(
                authorizedParties,
                services,
                count: 202,
                matchingIndexes: Enumerable.Range(0, 202).Where(x => x % 2 == 0))
            .ToList();
        var delegatedMatch = CreateDialog(allParties[101], services[0], index: 500, searchText: MatchTerm);
        var all = dialogs.Append(delegatedMatch).ToArray();

        await SeedDialogs(all, indexDialogs: true);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = authorizedParties.ToDictionary(
                x => x,
                _ => (IReadOnlySet<string>)services.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal),
            DialogIds = [delegatedMatch.Id]
        });

        var result = await Search(services: services, search: MatchTerm, limit: 300);

        AssertIds(result, ExpectedIds(all, x => x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_AndContentUpdatedRange_Should_IncludeDialogExactlyOnCutoff()
    {
        // Pins the inclusivity of the ContentUpdatedAfter bound (>=, not >). ContentUpdatedAt == CreatedAt
        // == DateAnchor + index minutes, so a cutoff equal to index 2's timestamp must include index 2.
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 4, matchingIndexes: [0, 1, 2, 3]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var cutoff = DateAnchor.AddMinutes(2);

        var result = await Search(parties: parties, search: MatchTerm, contentUpdatedAfter: cutoff);

        AssertIds(result, ExpectedIds(dialogs, x =>
            x.SearchText.Contains(MatchTerm, StringComparison.Ordinal) && x.CreatedAt >= cutoff));
    }

    [Fact]
    public async Task Search_WithFts_Should_TreatWhitespaceSeparatedTermsAsImplicitOr()
    {
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(
            parties,
            services,
            [
                (AlphaTerm, 0),
                (BravoTerm, 1),
                ($"{AlphaTerm} {BravoTerm}", 2),
                ("charlie", 3)
            ]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties, search: $"{AlphaTerm} {BravoTerm}");

        AssertIds(result, ExpectedIds(dialogs, x =>
            x.SearchText.Contains(AlphaTerm, StringComparison.Ordinal)
            || x.SearchText.Contains(BravoTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_Should_PreserveExplicitAndOperator()
    {
        var parties = CreateParties(1);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(
            parties,
            services,
            [
                (AlphaTerm, 0),
                (BravoTerm, 1),
                ($"{AlphaTerm} {BravoTerm}", 2)
            ]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await Search(parties: parties, search: $"{AlphaTerm} AND {BravoTerm}");

        AssertIds(result, ExpectedIds(dialogs, x =>
            x.SearchText.Contains(AlphaTerm, StringComparison.Ordinal)
            && x.SearchText.Contains(BravoTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_WithFts_Should_IncludeAuthorizedDelegatedDialogIds()
    {
        var parties = CreateParties(2);
        var services = CreateServices(1);
        var delegatedDialog = CreateDialog(parties[1], services[0], index: 0, searchText: DelegatedTerm);
        var directlyAuthorizedDialog = CreateDialog(parties[0], services[0], index: 1, searchText: DelegatedTerm);
        var dialogs = new[] { delegatedDialog, directlyAuthorizedDialog };

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [parties[0]] = new HashSet<string> { services[0] }
            },
            DialogIds = [delegatedDialog.Id]
        });

        var result = await Search(services: services, search: DelegatedTerm);

        AssertIds(result, ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_Pagination_Should_ReturnStablePages_ForFewPartiesWithoutFts()
    {
        var parties = CreateParties(2);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 7);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await SearchAllPages(parties: parties, pageSize: 2);

        result.Select(x => x.Id).Should().Equal(ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_Pagination_Should_ReturnStablePages_ForFewPartiesWithFts()
    {
        var parties = CreateParties(2);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 7, matchingIndexes: [0, 1, 3, 5]);

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await SearchAllPages(parties: parties, search: MatchTerm, pageSize: 2);

        result.Select(x => x.Id).Should().Equal(ExpectedIds(dialogs, x =>
            x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_Pagination_Should_ReturnStablePages_ForManyPartiesWithFts()
    {
        var parties = CreateParties(51);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 51, matchingIndexes: Enumerable.Range(0, 51));

        await SeedDialogs(dialogs, indexDialogs: true);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await SearchAllPages(services: services, search: MatchTerm, pageSize: 10);

        result.Select(x => x.Id).Should().Equal(ExpectedIds(dialogs, x =>
            x.SearchText.Contains(MatchTerm, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Search_Pagination_Should_ReturnStablePages_ForManyPartiesFewServicesWithoutFts()
    {
        var parties = CreateParties(101);
        var services = CreateServices(1);
        var dialogs = CreateDialogs(parties, services, count: 101);

        await SeedDialogs(dialogs);
        ConfigureSearchAuthorization(CreateAuthorization(parties, services));

        var result = await SearchAllPages(services: services, pageSize: 10);

        result.Select(x => x.Id).Should().Equal(ExpectedIds(dialogs));
    }

    [Fact]
    public async Task Search_Pagination_WithoutFts_AndMoreDelegatedDialogsThanPageSize_Should_InterleaveByOrder_ForMultiService()
    {
        // The delegated dialogs are UNION'd into the multi-service candidate set without a per-branch LIMIT.
        // This pins that, across page boundaries, they neither drown (when the service branches fill a page)
        // nor get stranded last - the outer ORDER BY ranks them with everything else - even when there are
        // more delegated dialogs than fit on a single page. Even indexes are authorized, odd indexes are
        // delegated, so the delegated dialogs are spread across the whole recency range (not all newest).
        var allParties = CreateParties(102);
        var authorizedParties = allParties[..101];
        var delegatedOwner = allParties[101];
        var services = CreateServices(2);

        var authorizedDialogs = Enumerable.Range(0, 150).Where(i => i % 2 == 0)
            .Select(i => CreateDialog(authorizedParties[i % authorizedParties.Length], services[i % services.Length], index: i, searchText: $"ordinary {i}"))
            .ToArray();
        var delegatedDialogs = Enumerable.Range(0, 150).Where(i => i % 2 == 1)
            .Where((_, k) => k % 3 == 0) // spread across the full range
            .Select(i => CreateDialog(delegatedOwner, services[i % services.Length], index: i, searchText: $"ordinary {i}"))
            .ToArray();
        var all = authorizedDialogs.Concat(delegatedDialogs).ToArray();
        delegatedDialogs.Length.Should().BeGreaterThan(10, "the delegated set must span multiple pages for this test to be meaningful");

        await SeedDialogs(all);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = authorizedParties.ToDictionary(
                x => x,
                _ => (IReadOnlySet<string>)services.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal),
            DialogIds = delegatedDialogs.Select(x => x.Id).ToList()
        });

        var result = await SearchAllPages(services: services, pageSize: 10);

        var orderedIds = result.Select(x => x.Id).ToList();
        orderedIds.Should().Equal(ExpectedIds(all));
    }

    [Fact]
    public async Task Search_Pagination_WithFts_AndMoreDelegatedDialogsThanPageSize_Should_InterleaveByOrder_ForMultiService()
    {
        // FTS counterpart of the above (MultiServiceFtsStrategy shares the same delegated-UNION tail): the
        // delegated matches must interleave by order across pages even when they outnumber the page size.
        var allParties = CreateParties(102);
        var authorizedParties = allParties[..101];
        var delegatedOwner = allParties[101];
        var services = CreateServices(2);

        var authorizedDialogs = Enumerable.Range(0, 150).Where(i => i % 2 == 0)
            .Select(i => CreateDialog(authorizedParties[i % authorizedParties.Length], services[i % services.Length], index: i, searchText: $"{MatchTerm} {i}"))
            .ToArray();
        var delegatedDialogs = Enumerable.Range(0, 150).Where(i => i % 2 == 1)
            .Where((_, k) => k % 3 == 0) // spread across the full range
            .Select(i => CreateDialog(delegatedOwner, services[i % services.Length], index: i, searchText: $"{MatchTerm} {i}"))
            .ToArray();
        var all = authorizedDialogs.Concat(delegatedDialogs).ToArray();
        delegatedDialogs.Length.Should().BeGreaterThan(10, "the delegated set must span multiple pages for this test to be meaningful");

        await SeedDialogs(all, indexDialogs: true);
        ConfigureSearchAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = authorizedParties.ToDictionary(
                x => x,
                _ => (IReadOnlySet<string>)services.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal),
            DialogIds = delegatedDialogs.Select(x => x.Id).ToList()
        });

        var result = await SearchAllPages(services: services, search: MatchTerm, pageSize: 10);

        var orderedIds = result.Select(x => x.Id).ToList();
        orderedIds.Should().Equal(ExpectedIds(all));
    }

    private async Task SeedDialogs(IEnumerable<SearchDialogSeed> dialogs, bool indexDialogs = false)
    {
        foreach (var dialog in dialogs)
        {
            var result = await Application.Send(CreateDialogCommand(dialog), TestContext.Current.CancellationToken);
            result.Value.Should().BeOfType<CreateDialogSuccess>();
        }

        if (indexDialogs)
        {
            await Application.PublishEvents();
        }
    }

    private async Task<PaginatedList<DialogDto>> Search(
        IReadOnlyCollection<string>? parties = null,
        IReadOnlyCollection<string>? services = null,
        string? search = null,
        int limit = 100,
        DateTimeOffset? contentUpdatedAfter = null)
    {
        var result = await Application.Send(CreateSearchQuery(
            parties,
            services,
            search,
            limit,
            continuationToken: null,
            contentUpdatedAfter: contentUpdatedAfter), TestContext.Current.CancellationToken);

        return result.Value.Should().BeOfType<PaginatedList<DialogDto>>().Subject;
    }

    private async Task<List<DialogDto>> SearchAllPages(
        IReadOnlyCollection<string>? parties = null,
        IReadOnlyCollection<string>? services = null,
        string? search = null,
        int pageSize = 2)
    {
        List<DialogDto> results = [];
        string? continuationToken = null;
        bool hasNextPage;

        do
        {
            var page = await Application.Send(CreateSearchQuery(
                parties,
                services,
                search,
                pageSize,
                continuationToken), TestContext.Current.CancellationToken);

            var typedPage = page.Value.Should().BeOfType<PaginatedList<DialogDto>>().Subject;
            results.AddRange(typedPage.Items);
            continuationToken = typedPage.ContinuationToken;
            hasNextPage = typedPage.HasNextPage;
        } while (hasNextPage);

        results.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        return results;
    }

    private static SearchDialogQuery CreateSearchQuery(
        IReadOnlyCollection<string>? parties,
        IReadOnlyCollection<string>? services,
        string? search,
        int limit,
        string? continuationToken,
        DateTimeOffset? contentUpdatedAfter = null)
    {
        var token = continuationToken is not null
                    && ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(
                        continuationToken,
                        out var parsedToken)
            ? parsedToken
            : null;

        return new SearchDialogQuery
        {
            Party = parties?.ToList(),
            ServiceResource = services?.ToList(),
            Search = search,
            Limit = limit,
            OrderBy = CreatedAtDescendingOrder(),
            ContinuationToken = token,
            ContentUpdatedAfter = contentUpdatedAfter
        };
    }

    private static CreateDialogCommand CreateDialogCommand(SearchDialogSeed dialog)
    {
        var dto = DialogGenerator.CreateSimpleDialogFaker
            .Clone()
            .UseSeed(dialog.Seed)
            .Generate();

        dto.Id = dialog.Id;
        dto.IdempotentKey = null;
        dto.Party = dialog.Party;
        dto.ServiceResource = dialog.ServiceResource;
        dto.CreatedAt = dialog.CreatedAt;
        dto.UpdatedAt = dialog.CreatedAt;
        dto.Status = DialogStatusInput.InProgress;
        dto.Content = new()
        {
            Title = CreateContentValue(dialog.SearchText)
        };

        return new CreateDialogCommand { Dto = dto };
    }

    private static ContentValueDto CreateContentValue(string value) =>
        new()
        {
            Value =
            [
                new LocalizationDto
                {
                    LanguageCode = "nb",
                    Value = value
                }
            ]
        };

    private static void ConfigureSearchAuthorization(DialogSearchAuthorizationResult authorization) =>
        DialogApplication.AltinnAuthorization.Configure(x =>
            x.ConfigureGetAuthorizedResourcesForSearch(authorization));

    private static void ConfigureApplicationSettings(int maxPartyFilterValues) =>
        DialogApplication.Settings.Set(TestApplicationSettings.CreateDefault(
            limits: new LimitsSettings
            {
                EndUserSearch = new EndUserSearchQueryLimits
                {
                    MaxPartyFilterValues = maxPartyFilterValues
                }
            }));

    private static DialogSearchAuthorizationResult CreateAuthorization(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> services) =>
        new()
        {
            ResourcesByParties = parties.ToDictionary(
                x => x,
                _ => (IReadOnlySet<string>)services.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal)
        };

    private static Guid[] ExpectedIds(
        IEnumerable<SearchDialogSeed> dialogs,
        Func<SearchDialogSeed, bool>? predicate = null) =>
        dialogs
            .Where(predicate ?? (_ => true))
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => x.Id)
            .ToArray();

    private static void AssertIds(PaginatedList<DialogDto> result, Guid[] expectedIds)
    {
        // Every test here expects at least one dialog, so an empty expected set means the seed/filter logic is broken, 
        // not that the search result is correct - fail fast with a clear message in that case.
        expectedIds.Should().NotBeEmpty();
        result.Items.Select(x => x.Id).Should().Equal(expectedIds);
        result.Items.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        result.HasNextPage.Should().BeFalse();
    }

    private static SearchDialogSeed[] CreateDialogs(
        string[] parties,
        string[] services,
        int count,
        IEnumerable<int>? matchingIndexes = null)
    {
        var matchingIndexSet = (matchingIndexes ?? []).ToHashSet();

        return Enumerable.Range(0, count)
            .Select(x => CreateDialog(
                party: parties[x % parties.Length],
                service: services[x % services.Length],
                index: x,
                searchText: matchingIndexSet.Contains(x) ? $"{MatchTerm} {x}" : $"ordinary {x}"))
            .ToArray();
    }

    private static SearchDialogSeed[] CreateDialogs(
        string[] parties,
        string[] services,
        IEnumerable<(string SearchText, int Index)> dialogs) =>
        dialogs
            .Select(x => CreateDialog(
                party: parties[x.Index % parties.Length],
                service: services[x.Index % services.Length],
                index: x.Index,
                searchText: x.SearchText))
            .ToArray();

    private static SearchDialogSeed CreateDialog(string party, string service, int index, string searchText)
    {
        var createdAt = DateAnchor.AddMinutes(index);
        return new(
            Id: NewUuidV7(createdAt),
            Party: party,
            ServiceResource: service,
            CreatedAt: createdAt,
            SearchText: searchText,
            Seed: index + 1);
    }

    private static string[] CreateServices(int count) =>
        Enumerable.Range(0, count)
            .Select(x => $"{ServicePrefix}-{x:D3}")
            .ToArray();

    private static string[] CreateParties(int count)
    {
        var parties = Enumerable.Range(1, count * 10)
            .Select(x => DialogGenerator.GenerateRandomParty(new Randomizer(x), forcePerson: true).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Take(count)
            .ToArray();

        return parties.Length == count
            ? parties
            : throw new InvalidOperationException($"Unable to generate {count} unique parties.");
    }

    private static OrderSet<SearchDialogQueryOrderDefinition, DialogEntity> CreatedAtDescendingOrder() =>
        OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse("createdAt", out var orderSet)
            ? orderSet
            : throw new InvalidOperationException("Unable to parse createdAt order.");

    private sealed record SearchDialogSeed(
        Guid Id,
        string Party,
        string ServiceResource,
        DateTimeOffset CreatedAt,
        string SearchText,
        int Seed);
}
