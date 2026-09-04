using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public static class AltinnAuthorizationExtensions
{
    /// <summary>
    /// Resolves the dialog details authorization and determines whether the user may access the dialog at all.
    /// Access is granted if the user has access to the dialog's main resource, or - if the user for some reason
    /// does not have access to the main resource, which might be because they are granted XACML-actions besides
    /// "read" not explicitly defined on the dialog - if the user has access to the dialog via the list authorization.
    /// </summary>
    /// <remarks>
    /// The resolved <see cref="DialogDetailsAuthorizationResult"/> is returned so callers can reuse it for
    /// per-resource decoration (e.g. flagging unauthorized actions or transmissions) without resolving it again.
    /// Callers should keep that decoration after this check, since it only answers whether the dialog is visible,
    /// not which sub-resources are authorized.
    /// </remarks>
    public static async Task<(bool HasAccess, DialogDetailsAuthorizationResult Authorization)> GetDialogAccess(
        this IAltinnAuthorization altinnAuthorization,
        DialogEntity dialog,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await altinnAuthorization.GetDialogDetailsAuthorization(dialog, cancellationToken);

        if (authorizationResult.HasAccessToMainResource())
        {
            return (true, authorizationResult);
        }

        var hasListAuthorization = await altinnAuthorization.HasListAuthorizationForDialog(dialog, cancellationToken);
        return (hasListAuthorization, authorizationResult);
    }
}
