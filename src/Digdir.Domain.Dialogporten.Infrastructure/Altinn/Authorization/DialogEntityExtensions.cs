using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class DialogEntityExtensions
{
    public static List<AltinnAction> GetAltinnActions(this DialogEntity dialogEntity)
    {
        // Get all authorization attributes grouped by action defined on the dialogEntity, including both
        // apiActions and guiActions, as well as dialog transmissions with authorization attributes (which
        // require authorization for the action "transmissionread" if not referring a separate resource).
        return dialogEntity.ApiActions
            .Select(x => new AltinnAction(x.Action!, x.AuthorizationAttribute))
            .Concat(dialogEntity.GuiActions
                .Select(x => new AltinnAction(x.Action!, x.AuthorizationAttribute)))
            .Concat(dialogEntity.Transmissions
                .Where(x => x.AuthorizationAttribute is not null)
                .Select(x => new AltinnAction(GetReadActionForAuthorizationAttribute(x.AuthorizationAttribute!), x.AuthorizationAttribute)))
            // We always need to check if the user can read the main resource
            .Append(new AltinnAction(Constants.ReadAction, Constants.MainResource))
            .GroupBy(x => new { x.Name, x.AuthorizationAttribute })
            .Select(g => g.First()) // Remove duplicates by grouping
            .ToList();
    }

    private static string GetReadActionForAuthorizationAttribute(string authorizationAttribute)
    {
        // Resource attributes may refer to either sub-resources/tasks that should be considered just another
        // attribute to be matched within the same policy file, or they may refer to separate resources (and policies).
        // In the former case, we need to use "transmissionread" as the action, as having "read" on the main resource would
        // also give access to the subresource/task. In the latter case, we should use "read", as the resource is a
        // separate entity.
        return authorizationAttribute.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? Constants.ReadAction
            : Constants.TransmissionReadAction;
    }
}
