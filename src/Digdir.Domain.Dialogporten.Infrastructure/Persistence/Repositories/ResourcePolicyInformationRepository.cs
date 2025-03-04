using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ResourcePolicyInformationRepository : IResourcePolicyInformationRepository
{
    private readonly DialogDbContext _dbContext;

    public ResourcePolicyInformationRepository(DialogDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DateTimeOffset> GetLastUpdatedAt(
        TimeSpan? timeSkew = null,
        CancellationToken cancellationToken = default)
    {
        var lastUpdatedAt = await _dbContext.ResourcePolicyInformation
            .Select(x => x.UpdatedAt)
            .DefaultIfEmpty()
            .MaxAsync(cancellationToken);

        return timeSkew.HasValue
            ? lastUpdatedAt.Add(timeSkew.Value)
            : lastUpdatedAt;
    }

    public async Task<int> Merge(IReadOnlyCollection<ResourcePolicyInformation> resourceMetadata, CancellationToken cancellationToken)
    {
        // language=sql
        const string sql =
            $"""
            with source as (
            	SELECT *
            	FROM unnest(@ids, @resources, @minimumAuthenticationLevels, @createdAts, @updatedAts) 
            	    as s(id, resource, minimumSecurityLevel, createdAt, updatedAt)
            )
            merge into "{nameof(ResourcePolicyInformation)}" t
            using source s
            on t."{nameof(ResourcePolicyInformation.Resource)}" = s.resource
            when matched then
              	update set 
              	    "{nameof(ResourcePolicyInformation.UpdatedAt)}" = s.updatedAt,
                    "{nameof(ResourcePolicyInformation.MinimumAuthenticationLevel)}" = s.minimumSecurityLevel
            when not matched then
              	insert ("{nameof(ResourcePolicyInformation.Id)}", "{nameof(ResourcePolicyInformation.Resource)}", "{nameof(ResourcePolicyInformation.MinimumAuthenticationLevel)}", "{nameof(ResourcePolicyInformation.CreatedAt)}", "{nameof(ResourcePolicyInformation.UpdatedAt)}")
              	values (s.id, s.resource, s.minimumSecurityLevel, s.createdAt, s.updatedAt);
            """;

        return resourceMetadata.Count == 0 ? 0
            : await _dbContext.Database.ExecuteSqlRawAsync(sql,
            [
                new NpgsqlParameter("ids", resourceMetadata.Select(x => x.Id).ToArray()),
                new NpgsqlParameter("resources", resourceMetadata.Select(x => x.Resource).ToArray()),
                new NpgsqlParameter("minimumAuthenticationLevels", resourceMetadata.Select(x => x.MinimumAuthenticationLevel).ToArray()),
                new NpgsqlParameter("createdAts", resourceMetadata.Select(x => x.CreatedAt).ToArray()),
                new NpgsqlParameter("updatedAts", resourceMetadata.Select(x => x.UpdatedAt).ToArray())
            ], cancellationToken);
    }
}
