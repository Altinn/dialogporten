using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal static partial class DialogEndUserSearchSqlHelpers
{
    internal static PostgresFormattableStringBuilder BuildPostPermissionFilters(
        GetDialogsQuery query,
        bool includeSearchFilter,
        bool includePaginationCondition)
    {
        var builder = new PostgresFormattableStringBuilder()
            .Append("WHERE 1=1");

        if (includeSearchFilter)
        {
            builder.AppendIf(query.Search is not null, """ AND ds."SearchVector" @@ ss.searchVector """);
        }

        builder
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
            .AppendSystemLabelFilterCondition(query.SystemLabel);

        if (includePaginationCondition)
        {
            builder.ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d");
        }

        return builder
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }

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

    internal static List<PartiesAndServices> BuildPartiesAndServices(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources)
    {
        var partiesAndServices = authorizedResources.ResourcesByParties
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

        return partiesAndServices;
    }

    internal static void LogPartiesAndServicesCount(
        ILogger logger,
        List<PartiesAndServices>? partiesAndServices)
    {
        if (partiesAndServices is null) return;
        if (!logger.IsEnabled(LogLevel.Information)) return;

        var totalPartiesCount = partiesAndServices.Sum(g => g.Parties.Length);
        var totalServicesCount = partiesAndServices.Sum(g => g.Services.Length);
        var groupsCount = partiesAndServices.Count;
        var groupSizes = partiesAndServices
            .Select(g => (g.Parties.Length, g.Services.Length))
            .ToList();

        LogPartiesAndServicesCount(logger, totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "PartiesAndServices: tp={TotalPartiesCount}, ts={TotalServicesCount}, g={GroupsCount}, gs={GroupSizes}")]
    private static partial void LogPartiesAndServicesCount(
        ILogger logger,
        int totalPartiesCount,
        int totalServicesCount,
        int groupsCount,
        List<(int PartiesCount, int ServicesCount)> groupSizes);

    internal static int GetTotalServiceCount(EndUserSearchContext context)
    {
        var uniqueServices = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        var constrainedParties = context.Query.Party;
        var constrainedServices = context.Query.ServiceResource;

        foreach (var (party, services) in context.AuthorizedResources.ResourcesByParties)
        {
            if (constrainedParties is not null && constrainedParties.Count != 0 && !constrainedParties.Contains(party))
            {
                continue;
            }

            foreach (var service in services)
            {
                if (constrainedServices is not null && constrainedServices.Count != 0 && !constrainedServices.Contains(service))
                {
                    continue;
                }

                uniqueServices.Add(service);
            }
        }

        return uniqueServices.Count;
    }
}
