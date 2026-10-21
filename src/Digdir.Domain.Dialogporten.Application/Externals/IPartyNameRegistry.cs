namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyNameRegistry
{

    /// <summary>
    /// Get Party Name
    /// </summary>
    ///
    /// <returns>The Name of the given Party, or null on any failures</returns>
    Task<string?> TryGetName(string externalIdWithPrefix, CancellationToken cancellationToken);

    /// <summary>
    ///Get Party Name
    /// </summary>
    /// <returns>The Name of the given Party</returns>
    Task<string> GetName(string externalIdWithPrefix, CancellationToken cancellationToken);
    void CacheName(string actorId, string name);
}
