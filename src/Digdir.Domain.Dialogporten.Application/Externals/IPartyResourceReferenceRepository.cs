namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyResourceReferenceRepository
{
    Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> resources,
        CancellationToken cancellationToken);

    Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken);
}
