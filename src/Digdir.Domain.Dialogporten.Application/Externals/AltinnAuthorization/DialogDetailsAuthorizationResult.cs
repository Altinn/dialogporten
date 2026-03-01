using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogDetailsAuthorizationResult
{
    // Each action applies to a resource. This is the main resource, another subresource indicated by a authorization attribute
    // e.g. "urn:altinn:subresource:some-sub-resource" or "urn:altinn:task:task_1", or another resource (i.e. policy)
    // e.g. urn:altinn:resource:some-other-resource
    public List<AltinnAction> AuthorizedAltinnActions { get; init; } = [];

    public bool HasAccessToMainResource() =>
        AuthorizedAltinnActions.Any(action => action.AuthorizationAttribute == Constants.MainResource);

    public bool HasAccessToGuiAction(string action, string? authorizationAttribute)
    {
        return authorizationAttribute is null
            ? HasAccessToMainResource()
            : HasAccessToAction(action, authorizationAttribute);
    }

    public bool HasAccessToApiAction(string action, string? authorizationAttribute)
    {
        return authorizationAttribute is null
            ? HasAccessToMainResource()
            : HasAccessToAction(action, authorizationAttribute);
    }

    public bool HasReadAccessToMainResource() =>
        AuthorizedAltinnActions.Any(x => x is
        {
            Name: Constants.ReadAction,
            AuthorizationAttribute: Constants.MainResource
        });

    public bool HasReadAccessToDialogTransmission(string? authorizationAttribute)
    {
        return authorizationAttribute is not null
            ? ( // Dialog transmissions are authorized by either the read or transmissionRead action, depending on the authorization attribute type
                // The infrastructure will ensure that the correct action is used, so here we just check for either
                AuthorizedAltinnActions.Contains(new(Constants.TransmissionReadAction, authorizationAttribute))
                || AuthorizedAltinnActions.Contains(new(Constants.ReadAction, authorizationAttribute))
            ) : HasAccessToMainResource();
    }

    private bool HasAccessToAction(string action, string authorizationAttribute) =>
        AuthorizedAltinnActions.Any(x => x.Name == action && x.AuthorizationAttribute == authorizationAttribute);
}
