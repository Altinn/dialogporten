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
                    GetReadActionForAuthorizationAttribute(authorizationAttribute),
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

    private static AuthorizationCheck FromContext(AuthorizationContext context, DialogEntity dialogEntity) =>
        new(context.Action ?? Constants.ReadAction,
            AuthorizationResourceSpec.FromContext(context.ServiceResource, context.AdditionalResourceAttribute),
            context.IncludeDialogParty ? context.Parties.Append(dialogEntity.Party) : context.Parties);

    // Resource attributes may refer to either sub-resources/tasks that should be considered just another
    // attribute to be matched within the same policy file, or they may refer to separate resources (and policies).
    // In the former case, we need to use "transmissionread" as the action, as having "read" on the main resource would
    // also give access to the subresource/task. In the latter case, we should use "read", as the resource is a
    // separate entity.
    public static string GetReadActionForAuthorizationAttribute(string authorizationAttribute) =>
        authorizationAttribute.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? Constants.ReadAction
            : Constants.TransmissionReadAction;
}
