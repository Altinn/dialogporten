namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

/// <summary>
/// The service resources the current end user may see.
/// When <see cref="IncludeFullCatalogue"/> is true, the caller should return the entire referenced catalogue and
/// <see cref="ResourceUrns"/> is empty. This happens when the caller is authorized to more parties than the
/// configured fallback limit, so the per-party authorized union is skipped. Otherwise <see cref="ResourceUrns"/>
/// is the distinct set of authorized and referenced resource URNs to return.
/// </summary>
public sealed record AuthorizedServiceResources(bool IncludeFullCatalogue, IReadOnlyCollection<string> ResourceUrns);

public interface IAuthorizedServiceResourcesProvider
{
    /// <summary>
    /// Resolves the service resources the current end user is authorized to and that are referenced by
    /// Dialogporten, optionally restricted to <paramref name="partyFilter"/> (party URNs not in the caller's
    /// authorized set are silently dropped). On an unfiltered request, if the caller is authorized to more
    /// parties than the configured limit, the per-party union is skipped and the result signals the full
    /// catalogue should be returned. The result is cached per (end user, normalized party filter); the value is
    /// bounded, so it stays small even for users authorized to very many parties.
    /// </summary>
    Task<AuthorizedServiceResources> GetAuthorizedServiceResources(
        string[]? partyFilter,
        CancellationToken cancellationToken);
}
