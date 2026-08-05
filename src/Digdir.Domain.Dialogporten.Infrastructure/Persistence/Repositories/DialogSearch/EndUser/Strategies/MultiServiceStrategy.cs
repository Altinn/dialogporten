using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class MultiServiceStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<MultiServiceStrategy> _logger;

    public MultiServiceStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<MultiServiceStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Non-FTS service-driven strategy for broad multi-party searches with a small effective service set.
    // Drives lookup by service resource to reduce party fan-out; each service group performs bounded
    // top-N probes before final merge.
    public string Name => nameof(MultiServiceStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is not null)
        {
            return QueryStrategyScores.Ineligible;
        }

        // effectiveServiceCount == 1 is handled by SingleServiceStrategy; this strategy is the preferred
        // driver only for genuinely multiple effective services (it remains an eligible fallback otherwise).
        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsMultiServiceEligible(
            context.EffectivePartyCount, context.EffectiveServiceCount, limits)
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Eligible;
    }

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is not null)
        {
            throw new InvalidOperationException("Free-text search is not supported by this strategy.");
        }

        var query = context.Query;
        var authorizedResources = context.AuthorizedResources;
        var partiesByService = DialogEndUserSearchSqlHelpers.BuildPartiesByService(authorizedResources);
        if (partiesByService.Count == 0)
        {
            throw new InvalidOperationException(
                "Multi-service strategy requires at least one authorized service with parties.");
        }

        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesByService, Name);

        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;

        // One scalar-service block per distinct service, UNION'd. Each block drives the recency-ordered
        // service index (scalar ServiceResource = bound param + bound text[] party filter) and
        // early-terminates at the page limit; the ordered branches are merge-appended and the outer LIMIT
        // early-terminates across services. Requires a custom plan (Npgsql default) so the bound party
        // arrays are estimated -- the planner then applies the party set as a filter on the service index
        // rather than probing the party-first index once per party.
        var builder = new PostgresFormattableStringBuilder()
            .Append("WITH candidate_dialogs AS (");

        var isFirstBranch = true;
        foreach (var (service, parties) in partiesByService)
        {
            builder.AppendIf(!isFirstBranch,
                """

                UNION ALL
                """);
            builder
                .Append(
                    $"""
                    (
                        SELECT d."Id", {orderColumnProjection}
                        FROM "Dialog" d
                        WHERE d."ServiceResource" = {service}
                          AND d."Party" = ANY({parties})
                          {dialogFilters}
                    """)
                .ApplyPaginationOrder(query.OrderBy!, alias: "d")
                .ApplyPaginationLimit(query.Limit)
                .Append(")");
            isFirstBranch = false;
        }

        builder.AppendIf(hasDelegatedDialogIds,
            $"""

            UNION
            (
                SELECT d."Id", {orderColumnProjection}
                FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                WHERE 1=1
                  {dialogFilters}
            )
            """);

        return builder
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
