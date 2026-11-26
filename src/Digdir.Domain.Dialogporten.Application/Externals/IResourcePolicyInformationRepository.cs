using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IResourcePolicyInformationRepository
{
    Task<int> Merge(IReadOnlyCollection<ResourcePolicyInformation> resourceMetadata, CancellationToken cancellationToken = default);
    Task<DateTimeOffset> GetLastUpdatedAt(TimeSpan? timeSkew = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, MinimumAuthenticationLevels>> GetMinimumAuthenticationLevels(CancellationToken cancellationToken);
}

/// <summary>
/// The minimum authentication levels a resource policy requires.
/// </summary>
/// <param name="AuthenticationLevel">The general level, from <c>urn:altinn:minimum-authenticationlevel</c>.</param>
/// <param name="SystemUserAuthenticationLevel">
/// The system-user level, from <c>urn:altinn:minimum-authenticationlevel-systemuser</c>, or null when the policy has none.
/// </param>
public sealed record MinimumAuthenticationLevels(int AuthenticationLevel, int? SystemUserAuthenticationLevel)
{
    /// <summary>
    /// Returns the level the caller must have. The system-user level can only relax the general level, never
    /// tighten it, as in the Altinn PEP: a system user passes when it meets either level.
    /// </summary>
    public int RequiredFor(bool isSystemUser) =>
        isSystemUser && SystemUserAuthenticationLevel is { } systemUserAuthenticationLevel
            ? Math.Min(AuthenticationLevel, systemUserAuthenticationLevel)
            : AuthenticationLevel;
}

public static class ResourcePolicyInformationExtensions
{
    public static ResourcePolicyInformation ToResourcePolicyInformation(this UpdatedResourcePolicyInformation updatedResourcePolicyInformation, DateTimeOffset createdAt)
    {
        return new ResourcePolicyInformation
        {
            Id = IdentifiableExtensions.CreateVersion7(),
            Resource = updatedResourcePolicyInformation.ResourceUrn.ToString()!,
            MinimumAuthenticationLevel = updatedResourcePolicyInformation.MinimumAuthenticationLevel,
            MinimumSystemUserAuthenticationLevel = updatedResourcePolicyInformation.MinimumSystemUserLevelAuthenticationLevel,
            CreatedAt = createdAt.ToUniversalTime(),
            UpdatedAt = updatedResourcePolicyInformation.UpdatedAt.ToUniversalTime()
        };
    }
}
