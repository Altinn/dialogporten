using System.Globalization;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class DbSetExtensions
{
    public static (string sql, object[] parameters) GeneratePrefilterAuthorizedDialogsSql(
        DialogSearchAuthorizationResult authorizedResources,
        DeletedFilter deletedFilter)
    {
        var parameters = new List<object>();
        var deletedFilterCondition = deletedFilter switch
        {
            DeletedFilter.Include => "",
            DeletedFilter.Exclude => "NOT \"Deleted\" AND ",
            DeletedFilter.Only => "\"Deleted\" AND",
            _ => throw new ArgumentOutOfRangeException(nameof(deletedFilter), deletedFilter, null)
        };

        var sb = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"""
                SELECT "Id", "ServiceResource"
                FROM "Dialog"
                WHERE {deletedFilterCondition} ("Id" = ANY(@p{parameters.Count})
                """);
        parameters.Add(authorizedResources.DialogIds);

        // Group parties that have the same resources
        var groupedResult = authorizedResources.ResourcesByParties
            .GroupBy(kv => kv.Value, new HashSetEqualityComparer<string>())
            .ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.Select(kv => kv.Key))
            );

        foreach (var (resources, parties) in groupedResult)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"""
                 OR (
                    "{nameof(DialogEntity.Party)}" = ANY(@p{parameters.Count}) 
                    AND "{nameof(DialogEntity.ServiceResource)}" = ANY(@p{parameters.Count + 1})
                 )
                 """);
            parameters.Add(parties);
            parameters.Add(resources);
        }

        sb.AppendLine(")");

        return (sb.ToString(), parameters.ToArray());
    }

    public static IQueryable<DialogEntity> PrefilterAuthorizedDialogs(
        this DbSet<DialogEntity> dialogs,
        DialogSearchAuthorizationResult authorizedResources,
        DeletedFilter? deletedFilter)
    {
        var (sql, parameters) = GeneratePrefilterAuthorizedDialogsSql(authorizedResources, deletedFilter ?? DeletedFilter.Exclude);
        return dialogs.FromSqlRaw(sql, parameters);
    }
}


public sealed class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
{
    public bool Equals(HashSet<T>? x, HashSet<T>? y)
    {
        return ReferenceEquals(x, y) || (x is not null && y is not null && x.SetEquals(y));
    }

    public int GetHashCode(HashSet<T> obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        unchecked
        {
            return obj.Aggregate(0, (hash, item) => hash ^ (item?.GetHashCode() ?? 0));
        }
    }
}
