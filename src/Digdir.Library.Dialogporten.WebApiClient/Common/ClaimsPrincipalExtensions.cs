using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten.Common;

internal static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public bool VerifyDialogId(Guid dialogId)
        {
            const string dialogIdClaimName = "i";
            return claimsPrincipal.TryGetClaimValue(dialogIdClaimName, out var dialogIdString)
                   && Guid.TryParse(dialogIdString, out var dialogIdClaim)
                   && dialogId == dialogIdClaim;
        }

        public bool VerifyActions(params string[] requiredActions)
        {
            const string actionsClaimName = "a";
            const string actionSeparator = ";";
            if (requiredActions.Length == 0)
            {
                return true;
            }

            if (!claimsPrincipal.TryGetClaimValue(actionsClaimName, out var actions))
            {
                return false;
            }

            var requiredActionsLength = requiredActions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var intersectionLength = actions
                .Split(actionSeparator)
                .Intersect(requiredActions, StringComparer.OrdinalIgnoreCase)
                .Count();
            return intersectionLength == requiredActionsLength;
        }

        public bool VerifyExpirationTime(IClock clock, TimeSpan clockSkew)
        {
            const string expirationTimeClaimName = "exp";
            var exp = claimsPrincipal.TryGetClaimValue(expirationTimeClaimName, out var exps)
                      && long.TryParse(exps, out var expl)
                ? DateTimeOffset.FromUnixTimeSeconds(expl).Add(clockSkew)
                : DateTimeOffset.MinValue;
            return clock.UtcNow <= exp;
        }

        public bool VerifyNotValidBefore(IClock clock, TimeSpan clockSkew)
        {
            const string notValidBeforeClaimName = "nbf";
            var nbf = claimsPrincipal.TryGetClaimValue(notValidBeforeClaimName, out var nbfs)
                      && long.TryParse(nbfs, out var nbfl)
                ? DateTimeOffset.FromUnixTimeSeconds(nbfl).Add(-clockSkew)
                : DateTimeOffset.MaxValue;
            return nbf <= clock.UtcNow;
        }

        public bool TryGetClaimValue(string claimType,
            [NotNullWhen(true)] out string? value)
        {
            value = claimsPrincipal.FindFirst(claimType)?.Value;
            return value is not null;
        }
    }
}
