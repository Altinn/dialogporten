using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SinglePartyNoInstanceNoFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    public string Name => "SinglePartyNoInstanceNoFts";

    public int Score(EndUserSearchContext context) =>
        context.Query.Search is null
        && context.AuthorizedResources.DialogIds.Count == 0
        && DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            context.Query,
            context.AuthorizedResources,
            out _)
            ? QueryStrategyScores.HighlyPreferred
            : QueryStrategyScores.Ineligible;

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is not null)
        {
            throw new InvalidOperationException("Free-text search is not supported by this strategy.");
        }

        if (context.AuthorizedResources.DialogIds.Count > 0)
        {
            throw new InvalidOperationException("Delegated dialog IDs are not supported by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
                context.Query,
                context.AuthorizedResources,
                out var authorization))
        {
            throw new InvalidOperationException("Single-party authorization is required for this strategy.");
        }

        var query = context.Query;

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d.*
                FROM "Dialog" d
                JOIN (
                    SELECT d."Id"
                    FROM "Dialog" d
                    WHERE d."Party" = {authorization.Party}
                """)
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{DialogEndUserSearchSqlHelpers.BuildDialogFilters(query)}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                ) AS sub ON d."Id" = sub."Id"
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }
}
