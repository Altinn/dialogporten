namespace Digdir.Domain.Dialogporten.Application.Externals;

/// <summary>
/// Reads service resource metadata and incremental authorization-related updates from Altinn resource registry through a single boundary,
/// giving consistent caching behavior and insulating feature code from upstream API shape changes.
/// </summary>
public interface IResourceRegistry
{
    /// <summary>
    /// Gets all service resources owned by the supplied organization number so callers can reuse one cached owner payload.
    /// </summary>
    Task<IReadOnlyCollection<ServiceResourceInformation>> GetResourceInformationForOrg(string orgNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Gets metadata for one service resource id.
    /// </summary>
    Task<ServiceResourceInformation?> GetResourceInformation(string serviceResourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Streams subject/resource relation changes since a point in time so synchronization jobs can process large deltas without loading all changes in memory.
    /// </summary>
    IAsyncEnumerable<List<UpdatedSubjectResource>> GetUpdatedSubjectResources(DateTimeOffset since, int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets changed resource policy information since a point in time.
    /// </summary>
    Task<IReadOnlyCollection<UpdatedResourcePolicyInformation>> GetUpdatedResourcePolicyInformation(DateTimeOffset since, int numberOfConcurrentRequests,
        CancellationToken cancellationToken);
}

public sealed record ServiceResourceInformation(
    string ResourceId,
    string ResourceType,
    string OwnerOrgNumber,
    string OwnOrgShortName,
    List<ResourceLocalization> DisplayName,
    List<ResourceLocalization> Description)
{
    public static readonly ServiceResourceInformation Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, [], []);
    public string ResourceType { get; } = ResourceType.ToLowerInvariant();
    public string OwnerOrgNumber { get; } = OwnerOrgNumber.ToLowerInvariant();
    public string OwnOrgShortName { get; } = OwnOrgShortName.ToLowerInvariant();
    public string ResourceId { get; } = ResourceId.ToLowerInvariant();
}

public sealed record ResourceLocalization(string LanguageCode, string Value);


public sealed record UpdatedSubjectResource(Uri SubjectUrn, Uri ResourceUrn, DateTimeOffset UpdatedAt, bool Deleted);
public sealed record UpdatedResourcePolicyInformation(Uri ResourceUrn, int MinimumAuthenticationLevel, DateTimeOffset UpdatedAt);
