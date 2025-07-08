using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface ISubjectResourceRepository
{
    Task<int> Merge(List<MergableSubjectResource> subjectResource, CancellationToken cancellationToken = default);
    Task<DateTimeOffset> GetLastUpdatedAt(TimeSpan? timeSkew = null, CancellationToken cancellationToken = default);
}

public sealed class MergableSubjectResource : SubjectResource
{
    public bool IsDeleted { get; set; }
}

public static class SubjectResourceExtensions
{
    public static MergableSubjectResource ToMergeableSubjectResource(this UpdatedSubjectResource subjectResource, DateTimeOffset createdAt)
    {
        return new MergableSubjectResource
        {
            Id = IdentifiableExtensions.CreateVersion7(),
            Subject = subjectResource.SubjectUrn.ToString()!,
            Resource = subjectResource.ResourceUrn.ToString()!,
            CreatedAt = createdAt.ToUniversalTime(),
            UpdatedAt = subjectResource.UpdatedAt.ToUniversalTime(),
            IsDeleted = subjectResource.Deleted
        };
    }
}
