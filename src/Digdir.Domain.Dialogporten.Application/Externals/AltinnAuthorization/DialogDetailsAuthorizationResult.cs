using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogDetailsAuthorizationResult
{
    /// <summary>
    /// The authorized checks, each carrying the subset of its parties the PDP permitted.
    /// A check applies to the main resource, a legacy authorization attribute
    /// (e.g. "urn:altinn:subresource:some-sub-resource", "urn:altinn:task:task_1" or
    /// "urn:altinn:resource:some-other-resource"), or an explicit authorization context.
    /// </summary>
    public List<AuthorizedCheck> AuthorizedChecks { get; init; } = [];

    /// <summary>
    /// Whether the given check (built by <see cref="AuthorizationCheckBuilder"/> from the same entity/dialog
    /// pair as the request) was authorized for at least one of its parties.
    /// </summary>
    public bool HasAccess(AuthorizationCheck check) =>
        AuthorizedChecks.Any(x => x.Check == check);

    public bool HasAccessToMainResource() =>
        AuthorizedChecks.Any(x => x.Check.Resource.Kind == AuthorizationResourceSpecKind.Main);

    /// <summary>
    /// Whether the requested action was permitted on the exact resource the legacy entity refers to:
    /// the main resource when no authorization attribute is given, otherwise that attribute.
    /// </summary>
    public bool HasAccessToAction(string requestedAction, string? authorizationAttribute) =>
        authorizationAttribute is null
            ? AuthorizedChecks.Any(x =>
                x.Check.Action == requestedAction
                && x.Check.Resource.Kind == AuthorizationResourceSpecKind.Main)
            : AuthorizedChecks.Any(x =>
                x.Check.Action == requestedAction
                && x.Check.Resource.Kind == AuthorizationResourceSpecKind.Legacy
                && x.Check.Resource.LegacyAuthorizationAttribute == authorizationAttribute);

    public bool HasReadAccessToMainResource() =>
        AuthorizedChecks.Any(x => x.Check is
        {
            Action: Constants.ReadAction,
            Resource.Kind: AuthorizationResourceSpecKind.Main
        });

    public bool HasReadAccessToDialogTransmission(string? authorizationAttribute)
    {
        // Dialog transmissions are authorized by either the read or transmissionRead action, depending on the
        // authorization attribute type. The infrastructure will ensure that the correct action is used, so here
        // we just check for either.
        return authorizationAttribute is not null
            ? AuthorizedChecks.Any(x =>
                x.Check.Action is Constants.TransmissionReadAction or Constants.ReadAction
                && x.Check.Resource.Kind == AuthorizationResourceSpecKind.Legacy
                && x.Check.Resource.LegacyAuthorizationAttribute == authorizationAttribute)
            : HasAccessToMainResource();
    }
}
