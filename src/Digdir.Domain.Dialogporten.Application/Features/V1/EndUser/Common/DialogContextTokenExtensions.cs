using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

/// <summary>
/// Shared context token issuance for the end user queries that expose authorization-context-carrying entities
/// (get-dialog, get-transmission and search-transmissions), so all of them apply the same issue/omit rules.
/// </summary>
internal static class DialogContextTokenExtensions
{
    /// <summary>
    /// Issues a token scoped to a single entity's PDP-verified grant, or null when the entity has no authorization
    /// context, is unauthorized, or its check cannot be found among the authorized checks. Reuses the check already
    /// computed for the authorization request — no additional PDP calls.
    /// </summary>
    public static string? GetContextTokenOrNull(
        this IDialogTokenGenerator dialogTokenGenerator,
        DialogEntity dialog,
        DialogDetailsAuthorizationResult authorization,
        bool isAuthorized,
        AuthorizationContext? context,
        AuthorizationCheck? check,
        Guid entityId,
        string entityType)
    {
        if (!isAuthorized || context is null || check is null)
        {
            return null;
        }

        // IsAuthorized for a context-carrying entity implies its check was authorized, so this
        // lookup cannot miss; guard anyway to fail closed rather than throw.
        var authorizedCheck = authorization.GetAuthorizedCheck(check);
        return authorizedCheck is null
            ? null
            : dialogTokenGenerator.GetDialogContextToken(dialog, authorizedCheck, entityId, entityType, DialogTokenIssuerVersion);
    }
}
