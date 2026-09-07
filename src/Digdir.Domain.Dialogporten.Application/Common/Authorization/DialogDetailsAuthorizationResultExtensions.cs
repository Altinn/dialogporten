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
/// Each predicate takes the entity's check, built by the caller via
/// <see cref="AuthorizationCheckBuilder.GetAuthorizationCheck(DialogTransmission, Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogEntity)"/>
/// and its overloads.
/// </summary>
public static class DialogDetailsAuthorizationResultExtensions
{
    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogApiAction apiAction, AuthorizationCheck? check) =>
        apiAction.AuthorizationContext is not null
            ? check is not null && authorization.HasAccess(check)
            : apiAction.EffectiveLegacyAction is { } action && authorization.HasAccessToAction(action, apiAction.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogGuiAction guiAction, AuthorizationCheck? check) =>
        guiAction.AuthorizationContext is not null
            ? check is not null && authorization.HasAccess(check)
            : guiAction.EffectiveLegacyAction is { } action && authorization.HasAccessToAction(action, guiAction.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogTransmission transmission, AuthorizationCheck? check) =>
        transmission.AuthorizationContext is not null
            ? check is not null && authorization.HasAccess(check)
            : authorization.HasReadAccessToDialogTransmission(transmission.AuthorizationAttribute);

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization, DialogAttachment attachment, AuthorizationCheck? check) =>
        // Dialog attachments without a context are never individually restricted; with a context,
        // read access to the dialog's main resource remains a precondition.
        attachment.AuthorizationContext is null
        || (authorization.HasReadAccessToMainResource() && check is not null && authorization.HasAccess(check));

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization,
        DialogTransmissionAttachment attachment,
        bool transmissionAuthorized,
        AuthorizationCheck? check) =>
        transmissionAuthorized
        && (attachment.AuthorizationContext is null || (check is not null && authorization.HasAccess(check)));

    public static bool HasAccess(
        this DialogDetailsAuthorizationResult authorization,
        DialogTransmissionNavigationalAction navigationalAction,
        bool transmissionAuthorized,
        AuthorizationCheck? check) =>
        transmissionAuthorized
        && (navigationalAction.AuthorizationContext is null || (check is not null && authorization.HasAccess(check)));

    /// <summary>
    /// Whether an unauthorized entity should be excluded from end user responses: removed from the
    /// collection it belongs to, leaving only a stub recording that it exists.
    /// </summary>
    public static bool ShouldExcludeWhenUnauthorized(this IAuthorizationContextCarrier carrier) =>
        carrier.AuthorizationContext?.UnauthorizedPresentation == AuthorizationContextUnauthorizedPresentation.Values.Excluded;
}
