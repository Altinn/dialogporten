using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class SubjectResourceRepository : ISubjectResourceRepository
{
    internal const string ReferencedPartyResourcesCacheName = "SubjectResourceReferencedPartyResources";

    private const string ReferencedPartyResourcesCacheKey = ReferencedPartyResourcesCacheName;

    private readonly DialogDbContext _dbContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFusionCache _referencedPartyResourcesCache;

    public SubjectResourceRepository(
        DialogDbContext dbContext,
        IServiceScopeFactory scopeFactory,
        IFusionCacheProvider cacheProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(cacheProvider);

        var referencedPartyResourcesCache = cacheProvider.GetCache(ReferencedPartyResourcesCacheName);
        ArgumentNullException.ThrowIfNull(referencedPartyResourcesCache);

        _dbContext = dbContext;
        _scopeFactory = scopeFactory;
        _referencedPartyResourcesCache = referencedPartyResourcesCache;
    }

    public async Task<Dictionary<string, List<string>>> GetSubjectsByResource(
        IReadOnlyCollection<string> resources,
        CancellationToken cancellationToken)
    {
        if (resources.Count == 0)
        {
            return [];
        }

        return (await _dbContext.SubjectResources
                .AsNoTracking()
                .Where(x => resources.Contains(x.Resource))
                .Select(x => new { x.Resource, x.Subject })
                .Distinct()
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.Resource)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.Subject).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetSubjectsForReferencedPartyResources(
        CancellationToken cancellationToken)
    {
        // The cached dictionary is treated as immutable and returned directly (no per-request copy).
        // Callers receive a read-only view, so the cached instance is protected from mutation.
        return await _referencedPartyResourcesCache.GetOrSetAsync<Dictionary<string, IReadOnlyList<string>>>(
            ReferencedPartyResourcesCacheKey,
            FetchSubjectsForReferencedPartyResources,
            token: cancellationToken);
    }

    private async Task<Dictionary<string, IReadOnlyList<string>>> FetchSubjectsForReferencedPartyResources(
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT sr."Resource", sr."Subject"
            FROM "SubjectResource" sr
            INNER JOIN partyresource."Resource" r
              ON sr."Resource" = 'urn:altinn:resource:' || r."UnprefixedResourceIdentifier"
            """;

        // Use a dedicated scope rather than the injected (request-scoped) DbContext. The factory can run detached
        // from the caller (soft-timeout background completion / eager refresh), after the request scope has been
        // disposed or while the caller is using its DbContext concurrently.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        return (await dbContext.Database
                .SqlQueryRaw<SubjectResourceRow>(sql)
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.Resource, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<string>)x.Select(y => y.Subject).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<DateTimeOffset> GetLastUpdatedAt(
        TimeSpan? timeSkew = null,
        CancellationToken cancellationToken = default)
    {
        var lastUpdatedAt = await _dbContext.SubjectResources
            .Select(x => x.UpdatedAt)
            .DefaultIfEmpty()
            .MaxAsync(cancellationToken);

        return timeSkew.HasValue
            ? lastUpdatedAt.Add(timeSkew.Value)
            : lastUpdatedAt;
    }

    public Task<DateTimeOffset> GetLastUpdatedAt(CancellationToken cancellationToken) =>
        _dbContext.SubjectResources
            .Select(x => x.UpdatedAt)
            .DefaultIfEmpty()
            .MaxAsync(cancellationToken);

    public async Task<int> Merge(List<MergableSubjectResource> subjectResource, CancellationToken cancellationToken)
    {
        const string sql =
            $"""
            with source as (
            	SELECT *
            	FROM unnest(@ids, @subjects, @resources, @createdAts, @updatedAts, @isDeletes) 
            	    as s(id, subject, resource, createdAt, updatedAt, isDeleted)
            )
            merge into "{nameof(SubjectResource)}" t
            using source s
            on t."{nameof(SubjectResource.Subject)}" = s.subject
            	AND t."{nameof(SubjectResource.Resource)}" = s.resource
            when matched AND s.isDeleted then 
            	delete
            when matched AND NOT s.isDeleted then
              	update set "{nameof(SubjectResource.UpdatedAt)}" = s.updatedAt
            when not matched AND NOT s.isDeleted then
              	insert ("{nameof(SubjectResource.Id)}", "{nameof(SubjectResource.Subject)}", "{nameof(SubjectResource.Resource)}", "{nameof(SubjectResource.CreatedAt)}", "{nameof(SubjectResource.UpdatedAt)}")
              	values (s.id, s.subject, s.resource, s.createdAt, s.updatedAt);
            """;

        if (subjectResource.Count == 0)
        {
            return 0;
        }

        var mergeCount = await _dbContext.Database.ExecuteSqlRawAsync(sql,
            [
                new NpgsqlParameter("ids", subjectResource.Select(x => x.Id).ToArray()),
                new NpgsqlParameter("subjects", subjectResource.Select(x => x.Subject).ToArray()),
                new NpgsqlParameter("resources", subjectResource.Select(x => x.Resource).ToArray()),
                new NpgsqlParameter("createdAts", subjectResource.Select(x => x.CreatedAt).ToArray()),
                new NpgsqlParameter("updatedAts", subjectResource.Select(x => x.UpdatedAt).ToArray()),
                new NpgsqlParameter("isDeletes", subjectResource.Select(x => x.IsDeleted).ToArray())
            ], cancellationToken);

        await _referencedPartyResourcesCache.ExpireAsync(
            ReferencedPartyResourcesCacheKey,
            token: cancellationToken);

        return mergeCount;
    }

    private sealed class SubjectResourceRow
    {
        public required string Resource { get; init; }
        public required string Subject { get; init; }
    }
}
