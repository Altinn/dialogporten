using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

/// <summary>
/// Normalizes dialog entities into <see cref="AuthorizationCheck"/>s — the single in-memory model used both to
/// build PDP requests and to look up authorization results at decoration time. Legacy authorization attributes
/// are carried opaquely (never re-classified into context fields) so their exact interpretation is preserved.
/// </summary>
public static class AuthorizationCheckBuilder
{
    /// <summary>
    /// Flattens the dialog aggregate into the distinct set of authorization checks to evaluate,
    /// always including a read check for the main resource.
    /// </summary>
    public static List<AuthorizationCheck> GetAuthorizationChecks(this DialogEntity dialogEntity)
    {
        var checks = new List<AuthorizationCheck?>();
        checks.AddRange(dialogEntity.ApiActions.Select(x => x.GetAuthorizationCheck(dialogEntity)));
        checks.AddRange(dialogEntity.GuiActions.Select(x => x.GetAuthorizationCheck(dialogEntity)));
        checks.AddRange(dialogEntity.Transmissions.Select(x => x.GetAuthorizationCheck(dialogEntity)));
        checks.AddRange(dialogEntity.Attachments.Select(x => x.GetAuthorizationCheck(dialogEntity)));
        checks.AddRange(dialogEntity.Transmissions
            .SelectMany(x => x.Attachments)
            .Select(x => x.GetAuthorizationCheck(dialogEntity)));
        checks.AddRange(dialogEntity.Transmissions
            .SelectMany(x => x.NavigationalActions)
            .Select(x => x.GetAuthorizationCheck(dialogEntity)));

        // We always need to check if the user can read the main resource
        checks.Add(dialogEntity.GetMainResourceReadCheck());

        return checks
            .OfType<AuthorizationCheck>()
            .Distinct()
            .ToList();
    }

    public static AuthorizationCheck GetMainResourceReadCheck(this DialogEntity dialogEntity) =>
        new(Constants.ReadAction, AuthorizationResourceSpec.Main, [dialogEntity.Party]);

    public static AuthorizationCheck? GetAuthorizationCheck(this DialogApiAction apiAction, DialogEntity dialogEntity) =>
        GetActionCheck(apiAction.EffectiveLegacyAction, apiAction.AuthorizationAttribute, apiAction.AuthorizationContext, dialogEntity);

    public static AuthorizationCheck? GetAuthorizationCheck(this DialogGuiAction guiAction, DialogEntity dialogEntity) =>
        GetActionCheck(guiAction.EffectiveLegacyAction, guiAction.AuthorizationAttribute, guiAction.AuthorizationContext, dialogEntity);

    public static AuthorizationCheck? GetAuthorizationCheck(this DialogTransmission transmission, DialogEntity dialogEntity) =>
        transmission.AuthorizationContext is { } context
            ? FromContext(context, dialogEntity)
            : transmission.AuthorizationAttribute is { } authorizationAttribute
                ? new AuthorizationCheck(
                    Constants.ReadAction,
                    AuthorizationResourceSpec.FromLegacyAuthorizationAttribute(authorizationAttribute),
                    [dialogEntity.Party])
                // Transmissions without authorization data piggyback on the main resource read check
                : null;

    public static AuthorizationCheck? GetAuthorizationCheck(this Attachment attachment, DialogEntity dialogEntity) =>
        attachment.AuthorizationContext is { } context
            ? FromContext(context, dialogEntity)
            : null;

    public static AuthorizationCheck? GetAuthorizationCheck(this DialogTransmissionNavigationalAction navigationalAction, DialogEntity dialogEntity) =>
        navigationalAction.AuthorizationContext is { } context
            ? FromContext(context, dialogEntity)
            : null;

    private static AuthorizationCheck? GetActionCheck(
        string? legacyAction,
        string? legacyAuthorizationAttribute,
        AuthorizationContext? context,
        DialogEntity dialogEntity)
    {
        if (context is not null)
        {
            return FromContext(context, dialogEntity);
        }

        // Write-side validation requires a legacy action when no context is supplied.
        return legacyAction is null
            ? null
            : new AuthorizationCheck(
                legacyAction,
                AuthorizationResourceSpec.FromLegacyAuthorizationAttribute(legacyAuthorizationAttribute),
                [dialogEntity.Party]);
    }

    // Each "parties" entry is evaluated at its own party with the context's right and no access restriction.
    // Each dialog party check is evaluated at the dialog's party with its own access restriction and its own
    // right, falling back to the context's: resource -> context's (-> the dialog's, when rendered), action ->
    // context's -> "read". The context's additional resource attribute is layered on every unit's resource.
    private static AuthorizationCheck FromContext(AuthorizationContext context, DialogEntity dialogEntity)
    {
        var contextAction = context.Action ?? Constants.ReadAction;
        var contextResource = AuthorizationResourceSpec.FromContext(context.ServiceResource, context.AdditionalResourceAttribute);

        var partyUnits = context.Parties
            .Select(party => new AuthorizationUnit(contextAction, contextResource, party));

        var dialogPartyUnits = context.DialogPartyChecks
            .Select(check => new AuthorizationUnit(
                check.Action ?? contextAction,
                AuthorizationResourceSpec.FromContext(
                    check.ServiceResource ?? context.ServiceResource,
                    context.AdditionalResourceAttribute),
                dialogEntity.Party,
                check.AccessRestriction.ToAuthorizationAccessRestriction(),
                check.ViaParty));

        return new AuthorizationCheck(partyUnits.Concat(dialogPartyUnits));
    }

    extension(DialogPartyCheckAccessRestriction.Values accessRestriction)
    {
        // Never default to None: an unknown restriction must not silently widen access.
        private AuthorizationAccessRestriction ToAuthorizationAccessRestriction() => accessRestriction switch
        {
            DialogPartyCheckAccessRestriction.Values.None => AuthorizationAccessRestriction.None,
            DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy => AuthorizationAccessRestriction.DirectAndHierarchy,
            DialogPartyCheckAccessRestriction.Values.ClientDelegation => AuthorizationAccessRestriction.ClientDelegation,
            _ => throw new UnreachableException($"Unknown dialog party check access restriction '{accessRestriction}'.")
        };
    }
}
