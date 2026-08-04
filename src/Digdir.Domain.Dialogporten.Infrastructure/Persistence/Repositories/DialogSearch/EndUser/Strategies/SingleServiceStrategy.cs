using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SingleServiceStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<SingleServiceStrategy> _logger;

    public SingleServiceStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<SingleServiceStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Non-FTS twin of SingleServiceFtsStrategy: a single effective service resource with a large
    // authorized party set, no free-text search. Drives the recency-ordered service index with a scalar
    // ServiceResource equality and a bound text[] party filter (early-terminating, log-friendly), with no
    // DialogSearch recheck.
    public string Name => nameof(SingleServiceStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is not null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsSingleServiceEligible(
            context.EffectivePartyCount, context.EffectiveServiceCount, limits)
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Ineligible;
    }

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is not null)
        {
            throw new InvalidOperationException("Free-text search is not supported by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSingleServiceAuthorization(
                context.AuthorizedResources,
                out var authorization))
        {
            throw new InvalidOperationException("Single-service authorization is required for this strategy.");
        }

        var query = context.Query;
        var delegatedDialogIds = context.AuthorizedResources.DialogIds.ToArray();
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, authorization, Name);

        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        var builder = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH candidates AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    WHERE d."ServiceResource" = {authorization.Service}
                      AND d."Party" = ANY({authorization.Parties})
                      {dialogFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                )
                """);

        // Late-materialize the page (optionally UNION'ing instance-delegated dialogs). Non-FTS: no @@
        // recheck, so ftsPredicate is null.
        return builder.AppendCandidatesDelegatedTail(
            query, dialogFilters, orderColumnProjection, orderColumnSelection, delegatedDialogIds, ftsPredicate: null);
    }
}
