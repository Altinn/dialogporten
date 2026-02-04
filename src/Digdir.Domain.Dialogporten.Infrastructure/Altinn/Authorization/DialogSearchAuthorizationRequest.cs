using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class DialogSearchAuthorizationRequest
{
    public required List<Claim> Claims { get; init; }
    public List<string> ConstraintParties { get; set; } = [];
    public List<string> ConstraintServiceResources { get; set; } = [];
}

internal static class DialogSearchAuthorizationRequestExtensions
{
    public static string GenerateCacheKey(this DialogSearchAuthorizationRequest request)
    {
        var claimsKey = string.Join(";", request.Claims.GetIdentifyingClaims()
            .Select(c => $"{c.Type}:{c.Value}"));

        var partiesKey = string.Join(";", request.ConstraintParties.OrderBy(p => p));
        var resourcesKey = string.Join(";", request.ConstraintServiceResources.OrderBy(r => r));

        var rawKey = $"{claimsKey}|{partiesKey}|{resourcesKey}";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var hashString = Convert.ToHexStringLower(hashBytes);

        return $"auth:search:{hashString}";
    }
}
