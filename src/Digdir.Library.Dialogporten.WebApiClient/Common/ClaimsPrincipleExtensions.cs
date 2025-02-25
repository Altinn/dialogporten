using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten.Common;

internal static class ClaimsPrincipleExtensions
{
    public static bool TryGetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType,
        [NotNullWhen(true)] out string? value)
    {
        var claim = claimsPrincipal.FindFirst(claimType);
        if (claim is null)
        {
            value = null;
            return false;
        }

        value = claim.Value;
        return true;
    }
}
