using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class DialogDetailsAuthorizationRequest
{
    public required ClaimsPrincipal ClaimsPrincipal { get; init; }
    public required string ServiceResource { get; init; }
    public required InstanceRef InstanceRef { get; init; }
    public required string Party { get; init; }

    // Each check applies an action to a resource on behalf of one or more parties. The resource is the main
    // resource, another resource indicated by a legacy authorization attribute (e.g.
    // "urn:altinn:subresource:some-sub-resource" or "urn:altinn:task:task_1"), or an explicit authorization context.
    public required List<AuthorizationCheck> Checks { get; init; }
}

internal static class DialogDetailsAuthorizationRequestExtensions
{
    public static string GenerateCacheKey(this DialogDetailsAuthorizationRequest request)
    {
        var claimsKey = string.Join(";", request.ClaimsPrincipal.Claims.GetIdentifyingClaims()
            .Select(c => $"{c.Type}:{c.Value}"));

        // The canonical identity covers every PDP-relevant input of a check (action, resource spec
        // and parties); leaving anything out of this key would let differing contexts share cached decisions.
        var checksKey = string.Join(";", request.Checks
            .Select(c => c.CanonicalIdentity)
            .Order(StringComparer.Ordinal));

        // CanonicalIdentity does not encode the dialog's main service resource for Main/Legacy checks
        // (it's a fixed "M" / "L:{attribute}"), but the PDP resource category is built from it, so it
        // must be included explicitly or differing service resources can share a cached decision.
        var rawKey = $"{request.InstanceRef.Value}||{claimsKey}|{request.ServiceResource}|{checksKey}";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var hashString = Convert.ToHexStringLower(hashBytes);

        return $"auth:details:{hashString}";
    }
}
