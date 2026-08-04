using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public interface IAltinnAuthorization
{
    Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken cancellationToken = default);

    Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(
        List<string> constraintParties,
        List<string> constraintServiceResources,
        bool includeDialogIds = true,
        // Overrides Limits.PartyResourcePruning.MinResourcesPruningThreshold for this call. Pass 0 to force
        // pruning regardless of result size (the authorized-service-resources endpoint requires the result to be
        // a subset of the referenced catalogue). Null keeps the configured threshold (the search optimization).
        int? minResourcesPruningThreshold = null,
        CancellationToken cancellationToken = default);

    Task<AuthorizedPartiesResult> GetAuthorizedParties(IPartyIdentifier authenticatedParty, bool flatten = false,
        CancellationToken cancellationToken = default);

    Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
        IPartyIdentifier authenticatedParty,
        List<string> constraintParties,
        CancellationToken cancellationToken = default);

    Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken);

    bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel);
    Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken);
}
