using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;

internal static class ActivityTypeAuthorization
{
    internal static bool UsingAllowedActivityTypes(
        IEnumerable<DialogActivityType.Values> dialogActivityTypes,
        IUser user, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (UsingCorrespondenceActivityTypes(dialogActivityTypes) && !IsCorrespondenceUser(user))
        {
            errorMessage = $"Use of {nameof(DialogActivityType.Values.CorrespondenceConfirmed)} " +
                           $"or {nameof(DialogActivityType.Values.CorrespondenceOpened)} " +
                           $"activity types requires the scope {AuthorizationScope.CorrespondenceScope}.";
        }

        return errorMessage == string.Empty;
    }

    private static bool UsingCorrespondenceActivityTypes(IEnumerable<DialogActivityType.Values> dialogActivityTypes)
        => dialogActivityTypes.Any(x => x is
            DialogActivityType.Values.CorrespondenceConfirmed or
            DialogActivityType.Values.CorrespondenceOpened);

    private static bool IsCorrespondenceUser(IUser user) =>
        user.GetPrincipal().HasScope(AuthorizationScope.CorrespondenceScope);
}
