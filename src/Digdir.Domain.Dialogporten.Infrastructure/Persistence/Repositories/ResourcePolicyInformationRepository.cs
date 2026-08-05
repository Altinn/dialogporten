using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ResourcePolicyInformationRepository : IResourcePolicyInformationRepository
{
    internal const string MinimumAuthenticationLevelsCacheName = "ResourcePolicyInformationMinimumAuthenticationLevels";

    private const string MinimumAuthenticationLevelsCacheKey = MinimumAuthenticationLevelsCacheName;

    private readonly DialogDbContext _dbContext;
    private readonly IFusionCache _minimumAuthenticationLevelsCache;

    public ResourcePolicyInformationRepository(
        DialogDbContext dbContext,
        IFusionCacheProvider cacheProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(cacheProvider);

        var minimumAuthenticationLevelsCache = cacheProvider.GetCache(MinimumAuthenticationLevelsCacheName);
        ArgumentNullException.ThrowIfNull(minimumAuthenticationLevelsCache);

        _dbContext = dbContext;
        _minimumAuthenticationLevelsCache = minimumAuthenticationLevelsCache;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetMinimumAuthenticationLevels(CancellationToken cancellationToken) =>
        await _minimumAuthenticationLevelsCache.GetOrSetAsync<Dictionary<string, int>>(
            MinimumAuthenticationLevelsCacheKey,
            FetchMinimumAuthenticationLevels,
            token: cancellationToken);

    private async Task<Dictionary<string, int>> FetchMinimumAuthenticationLevels(CancellationToken cancellationToken) =>
        await _dbContext.ResourcePolicyInformation
            .AsNoTracking()
            .Select(x => new { x.Resource, x.MinimumAuthenticationLevel })
            .ToDictionaryAsync(
                x => x.Resource,
                x => x.MinimumAuthenticationLevel,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

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

        if (resourceMetadata.Count == 0)
        {
            return 0;
        }

        var mergeCount = await _dbContext.Database.ExecuteSqlRawAsync(sql,
            [
                new NpgsqlParameter("ids", resourceMetadata.Select(x => x.Id).ToArray()),
                new NpgsqlParameter("resources", resourceMetadata.Select(x => x.Resource).ToArray()),
                new NpgsqlParameter("minimumAuthenticationLevels", resourceMetadata.Select(x => x.MinimumAuthenticationLevel).ToArray()),
                new NpgsqlParameter("createdAts", resourceMetadata.Select(x => x.CreatedAt).ToArray()),
                new NpgsqlParameter("updatedAts", resourceMetadata.Select(x => x.UpdatedAt).ToArray())
            ], cancellationToken);

        await _minimumAuthenticationLevelsCache.ExpireAsync(
            MinimumAuthenticationLevelsCacheKey,
            token: cancellationToken);

        return mergeCount;
    }
}
