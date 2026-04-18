using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SinglePartyFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<SinglePartyFtsStrategy> _logger;

    public SinglePartyFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<SinglePartyFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Specialized FTS strategy for a single effective party. It keeps the Dialog-first candidate
    // probe direct, avoiding the permission JSON/unnest machinery used by the generic FTS strategy.
    public string Name => nameof(SinglePartyFtsStrategy);

    public int Score(EndUserSearchContext context) =>
        context.Query.Search is not null
        && DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            context.AuthorizedResources,
            out _)
            ? QueryStrategyScores.HighlyPreferred
            : QueryStrategyScores.Ineligible;

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is null)
        {
            throw new InvalidOperationException("Free-text search is required by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            context.AuthorizedResources,
            out var authorization))
        {
            throw new InvalidOperationException("Single-party authorization is required for this strategy.");
        }

        var query = context.Query;
        var settings = _applicationSettings.Value.Limits.EndUserSearch;
        var delegatedDialogIds = context.AuthorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, authorization, Name);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var dialogCandidateOrderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "dc");
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH dialog_candidates AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    WHERE d."Party" = {authorization.Party}
                """)
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{dialogFilters}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .Append(
                $"""
                    LIMIT {settings.MaxFreeTextSearchCandidates}
                )
                ,fts_matches AS (
                    SELECT dc."Id", {dialogCandidateOrderColumnProjection}
                    FROM dialog_candidates dc
                    JOIN search."DialogSearch" ds ON ds."DialogId" = dc."Id"
                    WHERE {ftsPredicate}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "dc")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                )
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                ,delegated_dialogs AS (
                    SELECT d."Id", {orderColumnProjection}
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
                    SELECT "Id", {orderColumnSelection}
                    FROM fts_matches
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                    UNION
                    SELECT "Id", {orderColumnSelection}
                    FROM delegated_dialogs
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
}
