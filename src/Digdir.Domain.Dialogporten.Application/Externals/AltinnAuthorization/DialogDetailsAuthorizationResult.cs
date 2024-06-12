﻿using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogElements.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Elements;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogDetailsAuthorizationResult
{
    // Each action applies to a resource. This is the main resource, another subresource indicated by a authorization attribute
    // eg. "urn:altinn:subresource:some-sub-resource" or "urn:altinn:task:task_1", or another resource (ie. policy)
    // eg. urn:altinn:resource:some-other-resource
    public List<AltinnAction> AuthorizedAltinnActions { get; init; } = [];

    public bool HasReadAccessToMainResource() =>
        AuthorizedAltinnActions.Contains(new(Constants.ReadAction, Constants.MainResource));

    public bool HasReadAccessToDialogElement(DialogElement dialogElement) =>
        HasReadAccessToDialogElement(dialogElement.AuthorizationAttribute);

    public bool HasReadAccessToDialogElement(GetDialogDialogElementDto dialogElement) =>
        HasReadAccessToDialogElement(dialogElement.AuthorizationAttribute);

    private bool HasReadAccessToDialogElement(string? authorizationAttribute)
    {
        return authorizationAttribute is not null
            ? ( // Dialog elements are authorized by either the elementread or read action, depending on the authorization attribute type
                // The infrastructure will ensure that the correct action is used, so here we just check for either
                AuthorizedAltinnActions.Contains(new(Constants.ElementReadAction, authorizationAttribute))
                || AuthorizedAltinnActions.Contains(new(Constants.ReadAction, authorizationAttribute))
            ) : HasReadAccessToMainResource();
    }
}
