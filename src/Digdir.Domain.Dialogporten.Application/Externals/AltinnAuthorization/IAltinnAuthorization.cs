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
        CancellationToken cancellationToken = default);

    Task<AuthorizedPartiesResult> GetAuthorizedParties(IPartyIdentifier authenticatedParty, bool flatten = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken);

    bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel);
    Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken);
}
