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
/// Restricts how the end user's access at a party must have been obtained for an evaluation unit to permit.
/// Maps one to one to the PDP's "urn:altinn:authorization:access-restriction" values.
/// </summary>
public enum AuthorizationAccessRestriction
{
    /// <summary>No restriction; the attribute is omitted from the PDP request.</summary>
    None = 0,
    DirectAndHierarchy = 1,
    ClientDelegation = 2
}

/// <summary>
/// One PDP evaluation: is the user permitted to perform <see cref="Action"/> on <see cref="Resource"/> at
/// <see cref="Party"/>, considering only access obtained as allowed by <see cref="AccessRestriction"/>
/// (and, for <see cref="AuthorizationAccessRestriction.ClientDelegation"/>, through <see cref="ViaParty"/>)?
/// </summary>
public sealed record AuthorizationUnit(
    string Action,
    AuthorizationResourceSpec Resource,
    string Party,
    AuthorizationAccessRestriction AccessRestriction = AuthorizationAccessRestriction.None,
    string? ViaParty = null)
{
    /// <summary>Stable identity string covering every PDP-relevant input; used for cache keys and ordering.</summary>
    public string CanonicalIdentity =>
        $"{Action}\u001f{Resource.CanonicalIdentity}\u001f{Party}\u001f{AccessRestriction}\u001f{ViaParty}";
}

/// <summary>
/// A single authorization question for one entity: is the user permitted by at least one of <see cref="Units"/>?
/// Main resource and legacy checks consist of a single unit; an authorization context expands into one unit per
/// listed party and one per dialog party check, each possibly with its own action, resource and access restriction.
/// Value equality includes the (normalized) unit list, so a check built from an entity at decoration time
/// is identical to the check built for the PDP request.
/// </summary>
public sealed record AuthorizationCheck
{
    /// <summary>Distinct units, ordered by canonical identity. An empty list means the check can never be authorized (fail closed).</summary>
    public IReadOnlyList<AuthorizationUnit> Units { get; }

    public AuthorizationCheck(IEnumerable<AuthorizationUnit> units)
    {
        Units = units
            .Where(x => !string.IsNullOrWhiteSpace(x.Party))
            .DistinctBy(x => x.CanonicalIdentity, StringComparer.Ordinal)
            .OrderBy(x => x.CanonicalIdentity, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// A check of <paramref name="action"/> on <paramref name="resource"/> for each of <paramref name="parties"/>,
    /// without access restriction.
    /// </summary>
    public AuthorizationCheck(string action, AuthorizationResourceSpec resource, IEnumerable<string> parties)
        : this(parties.Select(party => new AuthorizationUnit(action, resource, party))) { }

    public bool Equals(AuthorizationCheck? other) =>
        other is not null && Units.SequenceEqual(other.Units);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var unit in Units)
        {
            hashCode.Add(unit);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>Stable identity string covering every PDP-relevant input; used for cache keys and ordering.</summary>
    public string CanonicalIdentity => string.Join('\u001d', Units.Select(x => x.CanonicalIdentity));
}

/// <summary>
/// An authorized check along with the subset of its units the PDP permitted.
/// </summary>
public sealed record AuthorizedCheck(AuthorizationCheck Check, IReadOnlyList<AuthorizationUnit> PermittedUnits)
{
    public static AuthorizedCheck FullyPermitted(AuthorizationCheck check) => new(check, check.Units);
}
