using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Models;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;

internal static partial class DialogEndUserSearchSqlHelpers
{
    internal static PostgresFormattableStringBuilder BuildDialogFilters(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .AppendManyFilter(query.Org, nameof(query.Org))
            .AppendManyFilter(query.Status, "StatusId", "int")
            .AppendManyFilter(query.ExtendedStatus, nameof(query.ExtendedStatus))
            .AppendIf(query.VisibleAfter is not null, $""" AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz) """)
            .AppendIf(query.ExpiresAfter is not null, $""" AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresAfter}::timestamptz) """)
            .AppendIf(query.Deleted is not null, $""" AND d."Deleted" = {query.Deleted}::boolean """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference}::text """)
            .AppendIf(query.CreatedAfter is not null, $""" AND {query.CreatedAfter}::timestamptz <= d."CreatedAt" """)
            .AppendIf(query.CreatedBefore is not null, $""" AND d."CreatedAt" <= {query.CreatedBefore}::timestamptz """)
            .AppendIf(query.UpdatedAfter is not null, $""" AND {query.UpdatedAfter}::timestamptz <= d."UpdatedAt" """)
            .AppendIf(query.UpdatedBefore is not null, $""" AND d."UpdatedAt" <= {query.UpdatedBefore}::timestamptz """)
            .AppendIf(query.ContentUpdatedAfter is not null, $""" AND {query.ContentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt" """)
            .AppendIf(query.ContentUpdatedBefore is not null, $""" AND d."ContentUpdatedAt" <= {query.ContentUpdatedBefore}::timestamptz """)
            .AppendIf(query.DueAfter is not null, $""" AND {query.DueAfter}::timestamptz <= d."DueAt" """)
            .AppendIf(query.DueBefore is not null, $""" AND d."DueAt" <= {query.DueBefore}::timestamptz """)
            .AppendIf(query.Process is not null, $""" AND d."Process" = {query.Process}::text """)
            .AppendIf(query.ExcludeApiOnly is not null, $""" AND ({query.ExcludeApiOnly}::boolean = false OR {query.ExcludeApiOnly}::boolean = true AND d."IsApiOnly" = false) """)
            .AppendSystemLabelMaskFilterCondition(query.SystemLabel)
            .AppendIsContentSeenFilterCondition(query.IsContentSeen)
            .AppendServiceOwnerLabelFilterCondition(query.ServiceOwnerLabels)
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d");

    internal static PostgresFormattableStringBuilder BuildOrderColumnProjection(IOrderSet<DialogEntity> orderSet, string alias) =>
        BuildColumnList(GetProjectedOrderColumnNames(orderSet), column => $"{alias}.\"{column}\"");

    internal static PostgresFormattableStringBuilder BuildOrderColumnSelection(IOrderSet<DialogEntity> orderSet) =>
        BuildColumnList(GetProjectedOrderColumnNames(orderSet), column => $"\"{column}\"");

    internal static List<PartiesAndServices> BuildPartiesAndServices(
        DialogSearchAuthorizationResult authorizedResources) => authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new PartiesAndServices(
                x.Select(k => k.party).ToArray(),
                x.Key.ToArray()))
            .Where(x => x.Parties.Length > 0 && x.Services.Length > 0)
            .ToList();

    // Inverts the authorization into one (service -> all authorized parties) entry per distinct service.
    // Used by the multi-service FTS strategy to emit one scalar-service block per service (each driving
    // the recency-ordered service index with a bound party param), UNION'd together.
    internal static IReadOnlyList<(string Service, string[] Parties)> BuildPartiesByService(
        DialogSearchAuthorizationResult authorizedResources) =>
        authorizedResources.ResourcesByParties
            .SelectMany(x => x.Value.Select(service => (Party: x.Key, Service: service)))
            .GroupBy(x => x.Service, StringComparer.Ordinal)
            .Select(g => (Service: g.Key, Parties: g.Select(x => x.Party).Distinct(StringComparer.Ordinal).ToArray()))
            .ToList();

    internal static int CountEffectiveServices(DialogSearchAuthorizationResult authorizedResources) =>
        authorizedResources.ResourcesByParties
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

    internal static int CountEffectiveParties(DialogSearchAuthorizationResult authorizedResources) =>
        authorizedResources.ResourcesByParties.Count(x => x.Value.Count > 0);

    // Shared eligibility boundaries for the service/party-driven strategies, kept in one place so a
    // threshold change is a single edit rather than lockstep edits across the 4 multi-* / 2 single-service
    // Score() methods. Each Score() combines these with its own FTS guard and score tier.

    // A large authorized party set funnelled through a small service set -> driving by service is better
    // than per-party probing. Also the condition under which the multi-party strategies *defer* to the
    // service-driven ones.
    internal static bool IsServiceDrivenTerritory(int effectivePartyCount, int effectiveServiceCount, EndUserSearchQueryLimits limits) =>
        effectivePartyCount > limits.MinServiceDrivenStrategyPartyCount
        && effectiveServiceCount <= limits.MaxServiceResourceFilterValues;

    // Exactly one effective service + a large party set -> SingleService(Fts)Strategy owns it (scalar
    // service + bound party param).
    internal static bool IsSingleServiceEligible(int effectivePartyCount, int effectiveServiceCount, EndUserSearchQueryLimits limits) =>
        effectiveServiceCount == 1
        && effectivePartyCount > limits.MinServiceDrivenStrategyPartyCount;

    // Two-to-MaxServiceResourceFilterValues effective services + a large party set -> MultiService(Fts)
    // Strategy owns it (per-service UNION).
    internal static bool IsMultiServiceEligible(int effectivePartyCount, int effectiveServiceCount, EndUserSearchQueryLimits limits) =>
        effectiveServiceCount > 1
        && IsServiceDrivenTerritory(effectivePartyCount, effectiveServiceCount, limits);

    internal static bool TryGetSinglePartyAuthorization(
        DialogSearchAuthorizationResult authorizedResources,
        [NotNullWhen(true)] out SinglePartyAndServices? authorization)
    {
        authorization = null;
        var effectiveAuthorizations = BuildPartiesAndServices(authorizedResources);
        if (effectiveAuthorizations.Sum(x => x.Parties.Length) != 1)
        {
            return false;
        }

        var effectiveAuthorization = effectiveAuthorizations.Single();
        authorization = new SinglePartyAndServices(
            effectiveAuthorization.Parties.Single(),
            effectiveAuthorization.Services);
        return true;
    }

    internal static void LogPartiesAndServicesCount(
        ILogger logger,
        List<PartiesAndServices>? partiesAndServices,
        string strategyName)
    {
        if (partiesAndServices is null) return;
        if (!logger.IsEnabled(LogLevel.Information)) return;

        var totalPartiesCount = partiesAndServices.Sum(g => g.Parties.Length);
        var totalServicesCount = partiesAndServices.Sum(g => g.Services.Length);
        var groupsCount = partiesAndServices.Count;
        var groupSizes = partiesAndServices
            .Select(g => (g.Parties.Length, g.Services.Length))
            .ToList();

        LogPartiesAndServicesCount(logger, strategyName, totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    internal static void LogPartiesAndServicesCount(
        ILogger logger,
        SinglePartyAndServices? partiesAndServices,
        string strategyName)
    {
        if (partiesAndServices is null) return;
        if (!logger.IsEnabled(LogLevel.Information)) return;

        const int totalPartiesCount = 1;
        const int groupsCount = 1;
        var totalServicesCount = partiesAndServices.Services.Length;
        List<(int PartiesCount, int ServicesCount)> groupSizes = [(1, totalServicesCount)];

        LogPartiesAndServicesCount(logger, strategyName, totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    // Count-based overload for the multi-service strategies: derives the diagnostic from the already-built
    // (service -> parties) inversion, avoiding a second full pass over the authorization. One "group" per
    // distinct service branch; group size = (parties in that branch, 1 service).
    internal static void LogPartiesAndServicesCount(
        ILogger logger,
        IReadOnlyList<(string Service, string[] Parties)> partiesByService,
        string strategyName)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        var totalPartiesCount = partiesByService.Sum(x => x.Parties.Length);
        var totalServicesCount = partiesByService.Count;
        var groupsCount = partiesByService.Count;
        var groupSizes = partiesByService.Select(x => (x.Parties.Length, 1)).ToList();

        LogPartiesAndServicesCount(logger, strategyName, totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    internal static bool TryGetSingleServiceAuthorization(
        DialogSearchAuthorizationResult authorizedResources,
        [NotNullWhen(true)] out SingleServiceAndParties? authorization)
    {
        authorization = null;
        // Bound the materialization to at most 2 — we only need to know whether there's exactly one
        // distinct service, not enumerate them all (a party can be authorized for many).
        var services = authorizedResources.ResourcesByParties
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToArray();
        if (services.Length != 1)
        {
            return false;
        }

        // effectiveServiceCount == 1 means every authorized party is authorized for that one service, so
        // the effective party set is simply all parties with at least one authorized service.
        var parties = authorizedResources.ResourcesByParties
            .Where(x => x.Value.Count > 0)
            .Select(x => x.Key)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (parties.Length == 0)
        {
            return false;
        }

        authorization = new SingleServiceAndParties(services[0], parties);
        return true;
    }

    internal static void LogPartiesAndServicesCount(
        ILogger logger,
        SingleServiceAndParties? serviceAndParties,
        string strategyName)
    {
        if (serviceAndParties is null) return;
        if (!logger.IsEnabled(LogLevel.Information)) return;

        var totalPartiesCount = serviceAndParties.Parties.Length;
        const int totalServicesCount = 1;
        const int groupsCount = 1;
        List<(int PartiesCount, int ServicesCount)> groupSizes = [(totalPartiesCount, totalServicesCount)];

        LogPartiesAndServicesCount(logger, strategyName, totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "PartiesAndServices: tp={TotalPartiesCount}, ts={TotalServicesCount}, g={GroupsCount}, gs={GroupSizes}, strategy={StrategyName}")]
    private static partial void LogPartiesAndServicesCount(
        ILogger logger,
        string strategyName,
        int totalPartiesCount,
        int totalServicesCount,
        int groupsCount,
        List<(int PartiesCount, int ServicesCount)> groupSizes);

    // Shared "candidates -> [delegated UNION] -> late-materialize" tail for the single-party /
    // single-service strategies, each having already appended a `candidates(Id, <order cols>)` CTE.
    //   * No delegated dialogs: `candidates` is already the ordered, limited page of ids; materialize the
    //     full Dialog rows directly.
    //   * Delegated dialogs present: UNION them into the candidate set (re-bound to a page, since they can
    //     interleave by recency), then late-materialize. A non-null ftsPredicate means the delegated
    //     dialogs are also @@-rechecked (FTS variants); null means non-FTS.
    internal static PostgresFormattableStringBuilder AppendCandidatesDelegatedTail(
        this PostgresFormattableStringBuilder builder,
        GetDialogsQuery query,
        PostgresFormattableStringBuilder dialogFilters,
        PostgresFormattableStringBuilder orderColumnProjection,
        PostgresFormattableStringBuilder orderColumnSelection,
        Guid[] delegatedDialogIds,
        PostgresFormattableStringBuilder? ftsPredicate)
    {
        if (delegatedDialogIds.Length == 0)
        {
            return builder
                .Append(
                    """
                    SELECT d.*
                    FROM candidates cd
                    JOIN "Dialog" d ON d."Id" = cd."Id"
                    """)
                .ApplyPaginationOrder(query.OrderBy!, alias: "d")
                .ApplyPaginationLimit(query.Limit);
        }

        builder
            .Append(
                $"""
                ,delegated_dialogs AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                    JOIN "Dialog" d ON d."Id" = dd."Id"
                """)
            .AppendIf(ftsPredicate is not null,
                $"""
                    JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                    WHERE {ftsPredicate}
                      {dialogFilters}
                """)
            .AppendIf(ftsPredicate is null,
                $"""
                    WHERE 1=1
                      {dialogFilters}
                """)
            .Append(
                $"""
                )
                ,candidate_dialogs AS (
                    SELECT "Id", {orderColumnSelection}
                    FROM candidates
                    UNION
                    SELECT "Id", {orderColumnSelection}
                    FROM delegated_dialogs
                )
                SELECT d.*
                FROM (
                    SELECT cd."Id"
                    FROM candidate_dialogs cd
                """);

        return builder
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

    private static PostgresFormattableStringBuilder BuildColumnList(
        IEnumerable<string> columns,
        Func<string, string> formatColumn)
    {
        var builder = new PostgresFormattableStringBuilder();
        var needsSeparator = false;

        foreach (var column in columns)
        {
            if (needsSeparator)
            {
                builder.Append(", ");
            }

            builder.Append(formatColumn(column));
            needsSeparator = true;
        }

        return builder;
    }

    private static IEnumerable<string> GetProjectedOrderColumnNames(IOrderSet<DialogEntity> orderSet) =>
        orderSet.Orders
            .Select(order => GetOrderColumnName(order.GetSelector().Body))
            .Where(column => column != nameof(DialogEntity.Id))
            .Distinct(StringComparer.Ordinal);

    private static string GetOrderColumnName(Expression expression) => expression switch
    {
        MemberExpression memberExpression => memberExpression.Member.Name,
        UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
        _ => throw new InvalidOperationException($"Unsupported order expression: {expression}")
    };
}
