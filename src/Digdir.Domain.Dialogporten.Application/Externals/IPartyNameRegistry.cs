namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyNameRegistry
{
    Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken);
    void CacheNames(Dictionary<string, string> nameByActorId);
}
