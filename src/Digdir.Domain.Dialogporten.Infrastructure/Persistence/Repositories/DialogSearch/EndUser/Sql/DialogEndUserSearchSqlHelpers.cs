using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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

    internal static int CountEffectiveServices(DialogSearchAuthorizationResult authorizedResources) =>
        authorizedResources.ResourcesByParties
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

    internal static int CountEffectiveParties(DialogSearchAuthorizationResult authorizedResources) =>
        authorizedResources.ResourcesByParties.Count(x => x.Value.Count > 0);

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
        var groupSizes = new List<(int PartiesCount, int ServicesCount)> { (1, totalServicesCount) };

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
