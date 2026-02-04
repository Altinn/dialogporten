using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class SubjectResourceRepository : ISubjectResourceRepository
{
    private readonly DialogDbContext _dbContext;

    public SubjectResourceRepository(DialogDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

        return subjectResource.Count == 0 ? 0
            : await _dbContext.Database.ExecuteSqlRawAsync(sql,
            [
                new NpgsqlParameter("ids", subjectResource.Select(x => x.Id).ToArray()),
                new NpgsqlParameter("subjects", subjectResource.Select(x => x.Subject).ToArray()),
                new NpgsqlParameter("resources", subjectResource.Select(x => x.Resource).ToArray()),
                new NpgsqlParameter("createdAts", subjectResource.Select(x => x.CreatedAt).ToArray()),
                new NpgsqlParameter("updatedAts", subjectResource.Select(x => x.UpdatedAt).ToArray()),
                new NpgsqlParameter("isDeletes", subjectResource.Select(x => x.IsDeleted).ToArray())
            ], cancellationToken);
    }
}
