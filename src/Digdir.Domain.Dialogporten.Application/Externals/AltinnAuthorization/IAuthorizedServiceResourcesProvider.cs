namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public interface IAuthorizedServiceResourcesProvider
{
    /// <summary>
    /// Returns the resources the current end user is authorized to, grouped by party URN. The result is cached
    /// per end user; the optional party filter is applied in-process by the caller, not here.
    /// </summary>
    Task<IReadOnlyDictionary<string, HashSet<string>>> GetAuthorizedServiceResourcesByParty(CancellationToken cancellationToken);
}
