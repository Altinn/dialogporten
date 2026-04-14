using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed partial class FreeTextSearchStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<FreeTextSearchStrategy> _logger;

    public FreeTextSearchStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<FreeTextSearchStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // FTS-specific strategy. It drives candidate lookup through the GIN index and caps the candidate
    // set before applying the wider dialog filters and final ordering.
    public string Name => "FreeTextSearch";

    public int Score(EndUserSearchContext context) =>
        context.Query.Search is not null
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Ineligible;

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is null)
        {
            throw new InvalidOperationException("Free-text search is required by this strategy.");
        }

        var query = context.Query;
        var authorizedResources = context.AuthorizedResources;
        var settings = _applicationSettings.Value.Limits.EndUserSearch;
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            authorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices, Name);

        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(query, authorizedResources);
        var candidateCap = settings.MaxFreeTextSearchCandidates;
        var candidateCapPerParty = GetCandidateLimitPerParty(
            effectivePartyCount,
            candidateCap,
            settings.MaxFreeTextSearchCandidatesPerParty,
            query.Limit);
        var ftsQuery = CreateFreeTextSearchQuery(query);
        var ftsPredicate = BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        var orderColumnProjection = BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var orderColumnSelection = BuildOrderColumnSelection(query.OrderBy!);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH permission_groups AS (
                    SELECT x."Parties" AS parties
                         , x."Services" AS services
                    FROM jsonb_to_recordset({JsonSerializer.Serialize(partiesAndServices)}::jsonb) AS x("Parties" text[], "Services" text[])
                )
                ,party_permissions AS (
                    SELECT p.party
                         , pg.services AS allowed_services
                    FROM permission_groups pg
                    CROSS JOIN LATERAL unnest(pg.parties) AS p(party)
                )
                ,authorized_parties AS (
                    SELECT DISTINCT party
                    FROM party_permissions
                )
                ,fts_candidates AS (
                    SELECT ds_inner."DialogId"
                         , ap.party
                    FROM authorized_parties ap
                    CROSS JOIN LATERAL (
                        SELECT ds."DialogId"
                        FROM search."DialogSearch" ds
                        WHERE ds."Party" = ap.party
                          AND {ftsPredicate}
                        LIMIT {candidateCapPerParty}
                    ) ds_inner
                    LIMIT {candidateCap}
                )
                ,fts_matches AS (
                    SELECT d."Id"
                         {orderColumnProjection}
                    FROM fts_candidates fc
                    JOIN party_permissions pp ON pp.party = fc.party
                    JOIN "Dialog" d ON d."Id" = fc."DialogId"
                    WHERE d."ServiceResource" = ANY(pp.allowed_services)
                      {dialogFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                $"""
                )
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                ,delegated_dialogs AS (
                    SELECT d."Id"
                         {orderColumnProjection}
                    FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                    JOIN "Dialog" d ON d."Id" = dd."Id"
                    JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                    WHERE {ftsPredicate}
                      {dialogFilters}
                )
                """)
            .Append(
                $"""
                ,candidate_dialogs AS (
                    SELECT "Id"{orderColumnSelection} FROM fts_matches
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                    UNION
                    SELECT "Id"{orderColumnSelection} FROM delegated_dialogs
                """)
            .Append(
                """
                )
                SELECT d.*
                FROM (
                    SELECT cd."Id"
                    FROM candidate_dialogs cd
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "cd")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                ) cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }

    private static int GetCandidateLimitPerParty(
        int effectivePartyCount,
        int candidateCap,
        int maxCandidateCapPerParty,
        int pageSize)
    {
        if (effectivePartyCount <= 0)
        {
            return pageSize + 1;
        }

        var fairShare = (int)Math.Ceiling((double)candidateCap / effectivePartyCount);
        var perPartyLimit = Math.Max(pageSize + 1, fairShare);
        return Math.Min(maxCandidateCapPerParty, perPartyLimit);
    }

    private static PostgresFormattableStringBuilder BuildOrderColumnProjection(IOrderSet<DialogEntity> orderSet, string alias) =>
        GetOrderColumnNames(orderSet)
            .Where(column => column != nameof(DialogEntity.Id))
            .Aggregate(new PostgresFormattableStringBuilder(), (builder, column) =>
                builder.Append((string)$"{Environment.NewLine}                         , {alias}.\"{column}\""));

    private static PostgresFormattableStringBuilder BuildOrderColumnSelection(IOrderSet<DialogEntity> orderSet) =>
        GetOrderColumnNames(orderSet)
            .Where(column => column != nameof(DialogEntity.Id))
            .Aggregate(new PostgresFormattableStringBuilder(), (builder, column) =>
                builder.Append((string)$"{Environment.NewLine}                         , \"{column}\""));

    private static IEnumerable<string> GetOrderColumnNames(IOrderSet<DialogEntity> orderSet) =>
        orderSet.Orders
            .Select(order => GetOrderColumnName(order.GetSelector().Body))
            .Distinct(StringComparer.Ordinal);

    private static string GetOrderColumnName(Expression expression) => expression switch
    {
        MemberExpression memberExpression => memberExpression.Member.Name,
        UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
        _ => throw new InvalidOperationException($"Unsupported order expression: {expression}")
    };

    private static FreeTextSearchQuery CreateFreeTextSearchQuery(GetDialogsQuery query) =>
        new(
            TsConfigName: GetTsConfigName(query.SearchLanguageCode),
            SearchString: BuildOrSearchString(query.Search!));

    private static PostgresFormattableStringBuilder BuildFreeTextSearchPredicate(FreeTextSearchQuery ftsQuery) =>
        new PostgresFormattableStringBuilder()
            .Append(
                $"""
                numnode(websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)) > 0
                AND querytree(websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)) <> 'T'
                AND ds."SearchVector" @@ websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)
                """);

    private static string BuildOrSearchString(string search)
    {
        var terms = SearchTermRegex()
            .Matches(search)
            .Select(match => match.Value)
            .Where(term => !term.Equals("OR", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return terms.Length == 0
            ? search
            : string.Join(" OR ", terms);
    }

    private static string GetTsConfigName(string? languageCode) => languageCode switch
    {
        "ar" => "arabic",
        "hy" => "armenian",
        "eu" => "basque",
        "ca" => "catalan",
        "da" => "danish",
        "nl" => "dutch",
        "en" => "english",
        "fi" => "finnish",
        "fr" => "french",
        "de" => "german",
        "el" => "greek",
        "hi" => "hindi",
        "hu" => "hungarian",
        "id" => "indonesian",
        "ga" => "irish",
        "it" => "italian",
        "lt" => "lithuanian",
        "ne" => "nepali",
        "nb" or "nn" or "no" => "norwegian",
        "pt" => "portuguese",
        "ro" => "romanian",
        "ru" => "russian",
        "sr" => "serbian",
        "es" => "spanish",
        "sv" => "swedish",
        "ta" => "tamil",
        "tr" => "turkish",
        "yi" => "yiddish",
        _ => "simple"
    };

    [GeneratedRegex("""(?:"[^"]+"|\S+)""", RegexOptions.CultureInvariant)]
    private static partial Regex SearchTermRegex();

    private sealed record FreeTextSearchQuery(string TsConfigName, string SearchString);
}
