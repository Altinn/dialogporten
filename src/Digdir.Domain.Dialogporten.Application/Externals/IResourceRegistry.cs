namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IResourceRegistry
{
    Task<IReadOnlyCollection<ServiceResourceInformation>> GetResourceInformationForOrg(string orgNumber, CancellationToken cancellationToken);
    Task<ServiceResourceInformation?> GetResourceInformation(string serviceResourceId, CancellationToken cancellationToken);
    IAsyncEnumerable<List<UpdatedSubjectResource>> GetUpdatedSubjectResources(DateTimeOffset since, int batchSize,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UpdatedResourcePolicyInformation>> GetUpdatedResourcePolicyInformation(DateTimeOffset since, int numberOfConcurrentRequests,
        CancellationToken cancellationToken);
}

public sealed record ServiceResourceInformation(string ResourceId, string ResourceType, string OwnerOrgNumber, string OwnOrgShortName)
{
    public static readonly ServiceResourceInformation Empty = new(string.Empty, string.Empty, string.Empty, string.Empty);
    public string ResourceType { get; } = ResourceType.ToLowerInvariant();
    public string OwnerOrgNumber { get; } = OwnerOrgNumber.ToLowerInvariant();
    public string OwnOrgShortName { get; } = OwnOrgShortName.ToLowerInvariant();
    public string ResourceId { get; } = ResourceId.ToLowerInvariant();
}


public sealed record UpdatedSubjectResource(Uri SubjectUrn, Uri ResourceUrn, DateTimeOffset UpdatedAt, bool Deleted);
public sealed record UpdatedResourcePolicyInformation(Uri ResourceUrn, int MinimumAuthenticationLevel, DateTimeOffset UpdatedAt);
