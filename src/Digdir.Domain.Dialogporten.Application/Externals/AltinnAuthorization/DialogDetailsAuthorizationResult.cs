using System.Runtime.Serialization;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogDetailsAuthorizationResult
{
    /// <summary>
    /// The authorized checks, each carrying the subset of its parties the PDP permitted.
    /// A check applies to the main resource, a legacy authorization attribute
    /// (e.g. "urn:altinn:subresource:some-sub-resource", "urn:altinn:task:task_1" or
    /// "urn:altinn:resource:some-other-resource"), or an explicit authorization context.
    /// </summary>
    public List<AuthorizedCheck> AuthorizedChecks { get; init; } = [];

    // Context-heavy dialogs can carry one distinct check per transmission, and the predicates below are
    // evaluated per entity when decorating query results — linear scans over AuthorizedChecks would make
    // decoration quadratic. Index the checks once on first use instead. Instances may be shared between
    // concurrent requests via the PDP cache; the benign race of building the index twice is acceptable as
    // each builder publishes a fully constructed, thereafter immutable structure.
    //
    // Excluded from the PDP cache's MessagePack serialization (ContractlessStandardResolverAllowPrivate
    // includes private fields): LookupIndex has no constructor parameter matching its members, which makes
    // the dynamic formatter throw on every write — even when this field is null — silently disabling the
    // cache. The index is cheap to rebuild lazily from AuthorizedChecks after deserialization.
    [IgnoreDataMember]
    private LookupIndex? _lookupIndex;

    private LookupIndex GetIndex() => _lookupIndex ??= new LookupIndex(AuthorizedChecks);

    /// <summary>
    /// Whether the given check (built by <see cref="AuthorizationCheckBuilder"/> from the same entity/dialog
    /// pair as the request) was authorized for at least one of its parties.
    /// </summary>
    public bool HasAccess(AuthorizationCheck check) =>
        GetIndex().AuthorizedByCheck.ContainsKey(check);

    /// <summary>
    /// The authorized check matching the given check, carrying the subset of parties the PDP permitted,
    /// or null if the check was not authorized for any party.
    /// </summary>
    public AuthorizedCheck? GetAuthorizedCheck(AuthorizationCheck check) =>
        GetIndex().AuthorizedByCheck.GetValueOrDefault(check);

    public bool HasAccessToMainResource() =>
        GetIndex().MainResourceActions.Count > 0;

    /// <summary>
    /// Whether the requested action was permitted on the exact resource the legacy entity refers to:
    /// the main resource when no authorization attribute is given, otherwise that attribute.
    /// </summary>
    public bool HasAccessToAction(string requestedAction, string? authorizationAttribute) =>
        authorizationAttribute is null
            ? GetIndex().MainResourceActions.Contains(requestedAction)
            : GetIndex().LegacyAttributeActions.Contains((requestedAction, authorizationAttribute));

    public bool HasReadAccessToMainResource() =>
        GetIndex().MainResourceActions.Contains(Constants.ReadAction);

    public bool HasReadAccessToDialogTransmission(string? authorizationAttribute) =>
        authorizationAttribute is not null
            ? GetIndex().LegacyAttributeActions.Contains((Constants.ReadAction, authorizationAttribute))
            : HasAccessToMainResource();

    private sealed class LookupIndex
    {
        public Dictionary<AuthorizationCheck, AuthorizedCheck> AuthorizedByCheck { get; }
        public HashSet<string> MainResourceActions { get; }
        public HashSet<(string Action, string Attribute)> LegacyAttributeActions { get; }

        public LookupIndex(List<AuthorizedCheck> authorizedChecks)
        {
            AuthorizedByCheck = new Dictionary<AuthorizationCheck, AuthorizedCheck>(authorizedChecks.Count);
            MainResourceActions = new HashSet<string>(StringComparer.Ordinal);
            LegacyAttributeActions = [];

            foreach (var authorizedCheck in authorizedChecks)
            {
                var check = authorizedCheck.Check;
                AuthorizedByCheck.TryAdd(check, authorizedCheck);
                switch (check.Resource)
                {
                    case { Kind: AuthorizationResourceSpecKind.Main }:
                        MainResourceActions.Add(check.Action);
                        break;
                    case { Kind: AuthorizationResourceSpecKind.Legacy, LegacyAuthorizationAttribute: { } attribute }:
                        LegacyAttributeActions.Add((check.Action, attribute));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
