using System.Security.Claims;
using System.Text.Json;

namespace Altinn.ApiClients.Dialogporten;

/// <summary>
/// Reads the authorization-context related claims of a validated dialog token.
/// </summary>
public static class DialogTokenClaimsPrincipalExtensions
{
    private const string AuthorizedEntitiesClaimName = "e";

    /// <summary>
    /// Whether the token's authorized entities ("e") claim contains the given entity reference: the id of an
    /// entity carrying an authorization context, or the "tokenRef" the service owner supplied on that context.
    /// The comparison is ordinal.
    /// </summary>
    public static bool VerifyEntityReference(this ClaimsPrincipal claimsPrincipal, string entityReference)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(entityReference);
        return claimsPrincipal.GetAuthorizedEntityReferences().Contains(entityReference, StringComparer.Ordinal);
    }

    /// <summary>
    /// The entity references listed in the token's authorized entities ("e") claim: for every authorization
    /// context the end user was authorized for, the id of the carrying entity or the service owner supplied
    /// "tokenRef". Empty when the claim is absent, i.e. the user is authorized for no context-carrying entity.
    /// </summary>
    public static IReadOnlyList<string> GetAuthorizedEntityReferences(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        // Array claims are carried as a single claim holding the raw JSON array.
        var json = claimsPrincipal.FindFirst(AuthorizedEntitiesClaimName)?.Value;
        if (json is null)
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
