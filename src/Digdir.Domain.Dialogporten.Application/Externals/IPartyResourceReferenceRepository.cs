namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyResourceReferenceRepository
{
    Task<IReadOnlyCollection<string>> GetReferencedResources(CancellationToken cancellationToken);

    Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> resources,
        CancellationToken cancellationToken);

    Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken);
}
