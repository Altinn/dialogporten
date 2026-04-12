using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Models;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;

internal static partial class DialogEndUserSearchSqlHelpers
{
    internal static PostgresFormattableStringBuilder BuildSearchJoin(bool includeSearch)
    {
        var builder = new PostgresFormattableStringBuilder();

        if (includeSearch)
        {
            builder.Append(
                """
                JOIN search."DialogSearch" ds ON d."Id" = ds."DialogId"
                CROSS JOIN searchString ss
                """);
        }

        return builder;
    }

    internal static PostgresFormattableStringBuilder BuildDialogFilters(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .AppendIf(query.Search is not null, """ AND ds."SearchVector" @@ ss.searchVector """)
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
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d");

    internal static List<PartiesAndServices> BuildPartiesAndServices(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources) => authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new PartiesAndServices(
                x.Select(k => k.party)
                    .Where(p => query.Party.IsNullOrEmpty() || query.Party.Contains(p))
                    .ToArray(),
                x.Key
                    .Where(s => query.ServiceResource.IsNullOrEmpty() || query.ServiceResource.Contains(s))
                    .ToArray()
               )
            )
            .Where(x => x.Parties.Length > 0 && x.Services.Length > 0)
            .ToList();

    internal static bool HasServiceResourceFilter(GetDialogsQuery query) => query.ServiceResource?.Count > 0;

    internal static int CountEffectiveParties(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources)
    {
        var queryParties = query.Party?.Count > 0
            ? query.Party.ToHashSet(StringComparer.Ordinal)
            : null;
        var queryServices = query.ServiceResource?.Count > 0
            ? query.ServiceResource.ToHashSet(StringComparer.Ordinal)
            : null;

        return authorizedResources.ResourcesByParties.Count(x =>
            (queryParties is null || queryParties.Contains(x.Key))
            && (queryServices is null
                ? x.Value.Count > 0
                : x.Value.Any(queryServices.Contains)));
    }

    internal static bool TryGetSinglePartyAuthorization(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources,
        [NotNullWhen(true)] out SinglePartyAndServices? authorization)
    {
        authorization = null;

        var queryParties = query.Party?.Count > 0
            ? query.Party.ToHashSet(StringComparer.Ordinal)
            : null;
        var queryServices = query.ServiceResource?.Count > 0
            ? query.ServiceResource.ToHashSet(StringComparer.Ordinal)
            : null;

        if (query.Party is [var party])
        {
            return TryCreateSinglePartyAuthorization(
                party,
                authorizedResources.ResourcesByParties,
                queryServices,
                out authorization);
        }

        var effectiveAuthorizations = authorizedResources.ResourcesByParties
            .Where(x => queryParties is null || queryParties.Contains(x.Key))
            .Select(x => CreateSinglePartyAuthorization(x.Key, x.Value, queryServices))
            .Where(x => x.Services.Length > 0)
            .Take(2)
            .ToArray();

        if (effectiveAuthorizations.Length != 1)
        {
            return false;
        }

        authorization = effectiveAuthorizations.Single();
        return true;
    }

    private static bool TryCreateSinglePartyAuthorization(
        string party,
        Dictionary<string, HashSet<string>> resourcesByParties,
        HashSet<string>? queryServices,
        [NotNullWhen(true)] out SinglePartyAndServices? authorization)
    {
        authorization = null;
        if (!resourcesByParties.TryGetValue(party, out var authorizedServices))
        {
            return false;
        }

        authorization = CreateSinglePartyAuthorization(party, authorizedServices, queryServices);
        return authorization.Services.Length > 0;
    }

    private static SinglePartyAndServices CreateSinglePartyAuthorization(
        string party,
        HashSet<string> authorizedServices,
        HashSet<string>? queryServices)
        => new(
            party,
            authorizedServices
                .Where(service => queryServices is null || queryServices.Contains(service))
                .ToArray());

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
        var groupSizes = new List<(int PartiesCount, int ServicesCount)> { (1, 1) };

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
}
