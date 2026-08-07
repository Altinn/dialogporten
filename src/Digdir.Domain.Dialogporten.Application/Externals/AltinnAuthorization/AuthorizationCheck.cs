using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public enum AuthorizationResourceSpecKind
{
    /// <summary>The dialog's own service resource (with instance reference).</summary>
    Main = 0,

    /// <summary>A legacy authorization attribute, carried opaquely and rendered with the legacy interpretation rules.</summary>
    Legacy = 1,

    /// <summary>An explicit authorization context with a resource override and/or an additional resource attribute.</summary>
    Context = 2
}

/// <summary>
/// Describes which resource attributes an authorization check applies to. Exactly one of three shapes:
/// the main resource, a legacy authorization attribute (kept raw to preserve legacy interpretation), or
/// explicit authorization context fields.
/// </summary>
public sealed record AuthorizationResourceSpec
{
    public AuthorizationResourceSpecKind Kind { get; init; }

    /// <summary>Raw legacy authorization attribute. Only set when <see cref="Kind"/> is <see cref="AuthorizationResourceSpecKind.Legacy"/>.</summary>
    public string? LegacyAuthorizationAttribute { get; init; }

    /// <summary>Resource override (replaces the dialog's resource and instance reference). Only set when <see cref="Kind"/> is <see cref="AuthorizationResourceSpecKind.Context"/>.</summary>
    public string? ServiceResource { get; init; }

    /// <summary>Additional resource attribute layered on top of the effective resource. Only set when <see cref="Kind"/> is <see cref="AuthorizationResourceSpecKind.Context"/>.</summary>
    public string? AdditionalResourceAttribute { get; init; }

    public static readonly AuthorizationResourceSpec Main = new();

    public static AuthorizationResourceSpec FromLegacyAuthorizationAttribute(string? authorizationAttribute) =>
        authorizationAttribute is null
            ? Main
            : new AuthorizationResourceSpec
            {
                Kind = AuthorizationResourceSpecKind.Legacy,
                LegacyAuthorizationAttribute = authorizationAttribute
            };

    public static AuthorizationResourceSpec FromContext(string? serviceResource, string? additionalResourceAttribute) =>
        new()
        {
            Kind = AuthorizationResourceSpecKind.Context,
            ServiceResource = serviceResource,
            AdditionalResourceAttribute = additionalResourceAttribute
        };

    /// <summary>
    /// Stable identity used for deduplication, deterministic ordering and cache keys. The kind tag ensures
    /// that a legacy attribute and a context expressing the same resource never collide.
    /// </summary>
    public string CanonicalIdentity => Kind switch
    {
        AuthorizationResourceSpecKind.Legacy => $"L:{LegacyAuthorizationAttribute}",
        AuthorizationResourceSpecKind.Context => $"C:{ServiceResource}\u001e{AdditionalResourceAttribute}",
        AuthorizationResourceSpecKind.Main => "M",
        _ => "M"
    };
}

/// <summary>
/// A single authorization question: is the user permitted to perform <see cref="Action"/> on the resource
/// described by <see cref="Resource"/>, on behalf of any of <see cref="Parties"/>?
/// Value equality includes the (normalized) party list, so a check built from an entity at decoration time
/// is identical to the check built for the PDP request.
/// </summary>
public sealed record AuthorizationCheck
{
    public string Action { get; }
    public AuthorizationResourceSpec Resource { get; }

    /// <summary>Sorted, distinct party URNs. An empty list means the check can never be authorized (fail closed).</summary>
    public IReadOnlyList<string> Parties { get; }

    public AuthorizationCheck(string action, AuthorizationResourceSpec resource, IEnumerable<string> parties)
    {
        Action = action;
        Resource = resource;
        Parties = parties
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public bool Equals(AuthorizationCheck? other) =>
        other is not null
        && string.Equals(Action, other.Action, StringComparison.Ordinal)
        && Resource == other.Resource
        && Parties.SequenceEqual(other.Parties, StringComparer.Ordinal);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Action, StringComparer.Ordinal);
        hashCode.Add(Resource);
        foreach (var party in Parties)
        {
            hashCode.Add(party, StringComparer.Ordinal);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>Stable identity string covering every PDP-relevant input; used for cache keys and ordering.</summary>
    public string CanonicalIdentity =>
        $"{Action}\u001f{Resource.CanonicalIdentity}\u001f{string.Join('\u001e', Parties)}";
}

/// <summary>
/// An authorized check along with the subset of its parties the PDP permitted.
/// </summary>
public sealed record AuthorizedCheck(AuthorizationCheck Check, IReadOnlyList<string> PermittedParties)
{
    public bool PermitsParty(string party) => PermittedParties.Contains(party, StringComparer.Ordinal);

    public static AuthorizedCheck FullyPermitted(AuthorizationCheck check) => new(check, check.Parties);
}
