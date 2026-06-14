using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class AuthorizedServiceResourcesProvider : IAuthorizedServiceResourcesProvider
{
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUser _user;
    private readonly IPartyResourceReferenceRepository _partyResourceReferenceRepository;

    public AuthorizedServiceResourcesProvider(
        IAltinnAuthorization altinnAuthorization,
        IUser user,
        IPartyResourceReferenceRepository partyResourceReferenceRepository)
    {
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);

        _altinnAuthorization = altinnAuthorization;
        _user = user;
        _partyResourceReferenceRepository = partyResourceReferenceRepository;
    }

    public async Task<Dictionary<string, HashSet<string>>> GetAuthorizedServiceResourcesByParty(
        CancellationToken cancellationToken)
    {
        // Validate the principal resolves to an end user; mirrors the dialog-search path, which treats a missing
        // end-user party identifier as unreachable. Done explicitly here so the failure is surfaced regardless of
        // the downstream authorization implementation.
        _ = _user.GetPrincipal().GetEndUserPartyIdentifierOrThrow();

        // The expensive Access Management call is already cached one level down (AuthorizedParties cache), so we
        // resolve directly here. DialogIds are not needed for this endpoint, so skip resolving them.
        var result = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            [], [], includeDialogIds: false, cancellationToken);

        // Unconditionally intersect with the resources actually referenced by Dialogporten. Unlike dialog search
        // (where pruning is a threshold-gated optimization), the result here IS the resource list, so it must not
        // leak resources outside the referenced catalogue. Passing threshold 0 forces pruning regardless of size.
        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            _partyResourceReferenceRepository,
            minResourcesPruningThreshold: 0,
            cancellationToken);

        return result.ResourcesByParties;
    }
}
