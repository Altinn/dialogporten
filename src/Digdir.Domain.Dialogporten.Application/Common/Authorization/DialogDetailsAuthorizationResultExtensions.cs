using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

/// <summary>
/// Per-entity access predicates over a <see cref="DialogDetailsAuthorizationResult"/>. Entities with an
/// authorization context are matched against their <see cref="AuthorizationCheckBuilder"/>-built check;
/// legacy entities use the legacy predicates. For child entities (attachments and navigational actions),
/// access to the parent is a precondition — a child context can only further restrict access, never widen it.
/// </summary>
public static class DialogDetailsAuthorizationResultExtensions
{
    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogApiAction apiAction, DialogEntity dialog) =>
        apiAction.AuthorizationContext is not null
            ? authorization.HasAccess(apiAction.GetAuthorizationCheck(dialog)!)
            : apiAction.Action is not null && authorization.HasAccessToAction(apiAction.Action, apiAction.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogGuiAction guiAction, DialogEntity dialog) =>
        guiAction.AuthorizationContext is not null
            ? authorization.HasAccess(guiAction.GetAuthorizationCheck(dialog)!)
            : guiAction.Action is not null && authorization.HasAccessToAction(guiAction.Action, guiAction.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogTransmission transmission, DialogEntity dialog) =>
        transmission.AuthorizationContext is not null
            ? authorization.HasAccess(transmission.GetAuthorizationCheck(dialog)!)
            : authorization.HasReadAccessToDialogTransmission(transmission.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogAttachment attachment, DialogEntity dialog) =>
        // Dialog attachments without a context are never individually restricted; with a context,
        // read access to the dialog's main resource remains a precondition.
        attachment.AuthorizationContext is null
        || (authorization.HasReadAccessToMainResource() && authorization.HasAccess(attachment.GetAuthorizationCheck(dialog)!));

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization,
        DialogTransmissionAttachment attachment,
        bool transmissionAuthorized,
        DialogEntity dialog) =>
        transmissionAuthorized
        && (attachment.AuthorizationContext is null || authorization.HasAccess(attachment.GetAuthorizationCheck(dialog)!));

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization,
        DialogTransmissionNavigationalAction navigationalAction,
        bool transmissionAuthorized,
        DialogEntity dialog) =>
        transmissionAuthorized
        && (navigationalAction.AuthorizationContext is null || authorization.HasAccess(navigationalAction.GetAuthorizationCheck(dialog)!));

    /// <summary>
    /// Whether an unauthorized entity should be redacted in end user responses: all content stripped,
    /// leaving only its existence and timestamps.
    /// </summary>
    public static bool ShouldRedactWhenUnauthorized(this IAuthorizationContextCarrier carrier) =>
        carrier.AuthorizationContext?.UnauthorizedPresentationId == AuthorizationContextUnauthorizedPresentation.Values.Redacted;
}
