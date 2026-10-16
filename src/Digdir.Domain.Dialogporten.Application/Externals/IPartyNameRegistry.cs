namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyNameRegistry
{
    Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken);
    Task<string> GetNameOrFail(string externalIdWithPrefix, CancellationToken cancellationToken);
    void CacheName(string actorId, string name);
}
