using System.Diagnostics;
using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

/// <summary>
/// A single (check, party) evaluation unit. <paramref name="ReferenceIndex"/> points into the
/// XACML MultiRequests reference list (and thereby the positionally-ordered PDP response).
/// </summary>
internal sealed record DecisionRequestTuple(AuthorizationCheck Check, string Party, int ReferenceIndex);

/// <summary>
/// The XACML request together with the tuple list it was built from. Request construction and
/// response mapping share this single ordering; the PDP response is required to contain exactly
/// one result per request reference, in order.
/// </summary>
internal sealed class PreparedDialogDetailsRequest
{
    public required XacmlJsonRequestRoot Request { get; init; }
    public required IReadOnlyList<DecisionRequestTuple> Tuples { get; init; }
    public required int ExpectedResults { get; init; }
}

internal static class DecisionRequestHelper
{
    private const string SubjectId = "s1";

    private const string PidClaimType = "pid";
    private const string UserIdClaimType = "urn:altinn:userid";
    private const string PartyIdClaimType = "urn:altinn:partyid";
    private const string RarAuthorizationDetailsClaimType = "authorization_details";

    private const string AttributeIdAction = "urn:oasis:names:tc:xacml:1.0:action:action-id";
    private const string AttributeIdResource = "urn:altinn:resource";
    private const string AttributeIdResourceInstance = "urn:altinn:resource:instance-id";
    private const string AttributeIdSubResource = "urn:altinn:subresource";

    private const string AttributeIdOrg = "urn:altinn:org";
    private const string AttributeIdApp = "urn:altinn:app";

    private const string AttributeIdUserId = "urn:altinn:userid";
    private const string AttributeIdPerson = "urn:altinn:person:identifier-no";
    private const string AttributeIdSystemUser = "urn:altinn:systemuser:uuid";

    // The order of these attribute types is important as we want to prioritize the most specific claim types.
    private static readonly List<string> PrioritizedClaimTypes = [AttributeIdUserId, AttributeIdPerson, AttributeIdSystemUser];

    private const string ReservedResourcePrefixForApps = "app_";

    private const string PermitResponse = "Permit";

    public static PreparedDialogDetailsRequest CreateDialogDetailsRequest(DialogDetailsAuthorizationRequest request)
    {
        // The PDP does not support self-identified users as parties, so we need to use the party-id claim instead.
        // Eventually, the PDP should support all external party identifiers, but until then we need to special case this.
        // The rewrite only ever applies to the authenticated user's own party; foreign context parties are passed
        // through as-is (an SI user cannot represent anyone else, so the PDP will simply deny — which is correct).
        var endUserPartyIdentifier = request.ClaimsPrincipal.GetEndUserPartyIdentifier();
        string? rewritablePartyId = null;
        if (endUserPartyIdentifier is AltinnSelfIdentifiedUserIdentifier or IdportenEmailUserIdentifier or FeideUserIdentifier
            && request.ClaimsPrincipal.TryGetPartyId(out var partyId))
        {
            rewritablePartyId = $"{PartyIdClaimType}:{partyId}";
        }

        // Expand every check into one evaluation unit per party, deterministically ordered. This ordering
        // defines the resource/action category numbering and the MultiRequests reference order, which the
        // (positional) response mapping depends on.
        var expandedTuples = request.Checks
            .SelectMany(check => check.Parties.Select(party => (Check: check, Party: party)))
            .Select(x => (
                x.Check,
                x.Party,
                EffectiveParty: rewritablePartyId is not null && x.Party == endUserPartyIdentifier!.FullId
                    ? rewritablePartyId
                    : x.Party))
            .OrderBy(x => x.Check.Action, StringComparer.Ordinal)
            .ThenBy(x => x.Check.Resource.CanonicalIdentity, StringComparer.Ordinal)
            .ThenBy(x => x.EffectiveParty, StringComparer.Ordinal)
            .ToList();

        var accessSubject = CreateAccessSubjectCategory(request.ClaimsPrincipal.Claims);

        var actionIdByName = new Dictionary<string, string>(StringComparer.Ordinal);
        var actionCategories = new List<XacmlJsonCategory>();
        var resourceIdByKey = new Dictionary<(AuthorizationResourceSpec Spec, string EffectiveParty), string>();
        var resourceCategories = new List<XacmlJsonCategory>();
        var referenceIndexByIds = new Dictionary<(string ResourceId, string ActionId), int>();
        var references = new List<XacmlJsonRequestReference>();
        var tuples = new List<DecisionRequestTuple>(expandedTuples.Count);

        foreach (var (check, party, effectiveParty) in expandedTuples)
        {
            if (!actionIdByName.TryGetValue(check.Action, out var actionId))
            {
                actionId = $"a{actionIdByName.Count + 1}";
                actionIdByName.Add(check.Action, actionId);
                actionCategories.Add(new XacmlJsonCategory
                {
                    Id = actionId,
                    Attribute = [new() { AttributeId = AttributeIdAction, Value = check.Action }]
                });
            }

            var resourceKey = (check.Resource, effectiveParty);
            if (!resourceIdByKey.TryGetValue(resourceKey, out var resourceId))
            {
                resourceId = $"r{resourceIdByKey.Count + 1}";
                resourceIdByKey.Add(resourceKey, resourceId);
                resourceCategories.Add(CreateResourceCategory(
                    resourceId, request.ServiceResource, request.InstanceRef, GetPartyAttribute(effectiveParty), check.Resource));
            }

            // Identical (resource, action) pairs collapse to a single PDP request; all tuples sharing
            // the pair map back to the same result.
            if (!referenceIndexByIds.TryGetValue((resourceId, actionId), out var referenceIndex))
            {
                referenceIndex = references.Count;
                referenceIndexByIds.Add((resourceId, actionId), referenceIndex);
                references.Add(new XacmlJsonRequestReference
                {
                    ReferenceId = [SubjectId, resourceId, actionId]
                });
            }

            tuples.Add(new DecisionRequestTuple(check, party, referenceIndex));
        }

        var xacmlJsonRequest = new XacmlJsonRequest
        {
            AccessSubject = accessSubject,
            Action = actionCategories,
            Resource = resourceCategories,
            MultiRequests = new XacmlJsonMultiRequests { RequestReference = references }
        };

        return new PreparedDialogDetailsRequest
        {
            Request = new XacmlJsonRequestRoot { Request = xacmlJsonRequest },
            Tuples = tuples,
            ExpectedResults = references.Count
        };
    }

    public static DialogDetailsAuthorizationResult CreateDialogDetailsResponse(
        PreparedDialogDetailsRequest preparedRequest, XacmlJsonResponse? xacmlJsonResponse)
    {
        var results = xacmlJsonResponse?.Response;

        // The XACML JSON profile guarantees one result per request reference, in request order.
        // Anything else means we cannot correlate decisions reliably: fail closed and deny everything.
        if (results is null || results.Count != preparedRequest.ExpectedResults)
        {
            return new DialogDetailsAuthorizationResult();
        }

        var authorizedChecks = preparedRequest.Tuples
            .Where(tuple => results[tuple.ReferenceIndex].Decision == PermitResponse)
            .GroupBy(tuple => tuple.Check)
            .Select(group => new AuthorizedCheck(
                group.Key,
                group.Select(tuple => tuple.Party).Distinct(StringComparer.Ordinal).ToList()))
            .ToList();

        return new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks = authorizedChecks
        };
    }

    private static List<XacmlJsonCategory> CreateAccessSubjectCategory(IEnumerable<Claim> claims) =>
        // The PDP expects for the most part only a single subject attribute, and will even fail the request
        // for some types (e.g. the urn:altinn:systemuser:uuid) if there are multiple subject attributes (for
        // security reasons). We therefore need to filter out the relevant attributes and only include those,
        // which in essence is the pid and the system user uuid. In addition, we also utilize urn:altinn:userid
        // if present instead of the pid as a simple optimization as this offloads the PDP from having to look up
        // the user id from the pid. See PrioritizedClaimTypes for the order of prioritization.
        claims.Select(claim => claim.Type switch
        {
            UserIdClaimType => new XacmlJsonCategory
            {
                Id = SubjectId,
                Attribute = [new() { AttributeId = AttributeIdUserId, Value = claim.Value }]
            },
            PidClaimType => new XacmlJsonCategory
            {
                Id = SubjectId,
                Attribute = [new() { AttributeId = AttributeIdPerson, Value = claim.Value }]
            },
            RarAuthorizationDetailsClaimType when claim.TryGetSystemUserId(out var systemUserId) => new XacmlJsonCategory
            {
                Id = SubjectId,
                Attribute =
                [
                    new XacmlJsonAttribute { AttributeId = AttributeIdSystemUser, Value = systemUserId }
                ]
            },
            _ => null
        })
        .Where(x => x != null)
        .MinBy(x => PrioritizedClaimTypes.IndexOf(x!.Attribute[0].AttributeId)) switch
        {
            { } validCategory => new List<XacmlJsonCategory> { validCategory },
            _ => throw new UnreachableException(
                "Unable to find a suitable subject attribute for the authorization request. Having a known user type should be enforced during authentication (see UserTypeValidationMiddleware)."),
        };

    private static XacmlJsonCategory CreateResourceCategory(
        string id,
        string serviceResource,
        InstanceRef instanceRef,
        XacmlJsonAttribute? partyAttribute,
        AuthorizationResourceSpec spec)
    {
        List<XacmlJsonAttribute> attributes = [];

        switch (spec.Kind)
        {
            case AuthorizationResourceSpecKind.Main:
                attributes.AddRange(GetAttributesForServiceResource(serviceResource));
                if (partyAttribute is not null) attributes.Add(partyAttribute);
                attributes.Add(new()
                {
                    AttributeId = AttributeIdResourceInstance,
                    Value = instanceRef.Value
                });
                // Preserve the legacy wire format: the "main" sentinel has always been rendered as an
                // extra urn:altinn:subresource attribute on the dialog's own resource.
                attributes.Add(new XacmlJsonAttribute
                {
                    AttributeId = AttributeIdSubResource,
                    Value = Constants.MainResource
                });
                break;

            case AuthorizationResourceSpecKind.Legacy:
                attributes.AddRange(GetAttributesForServiceResource(serviceResource));
                if (partyAttribute is not null) attributes.Add(partyAttribute);
                attributes.Add(new()
                {
                    AttributeId = AttributeIdResourceInstance,
                    Value = instanceRef.Value
                });

                var legacyAttributes = GetResourceAttributesForAuthorizationAttribute(spec.LegacyAuthorizationAttribute!);

                // If we get either urn:altinn:app/urn:altinn:org or urn:altinn:resource attributes, this should
                // be considered overrides that should be used instead of the default resource attributes.
                if (legacyAttributes.Any(x => x.AttributeId is AttributeIdApp or AttributeIdOrg or AttributeIdResource))
                {
                    attributes.RemoveAll(x => x.AttributeId is AttributeIdResource
                        or AttributeIdResourceInstance
                        or AttributeIdApp
                        or AttributeIdOrg
                    );
                }

                attributes.AddRange(legacyAttributes);
                break;

            case AuthorizationResourceSpecKind.Context:
                // A context resource override replaces the dialog's own resource, and the instance reference
                // no longer applies (it belongs to the dialog's resource). This mirrors the legacy
                // urn:altinn:resource/urn:altinn:app override semantics.
                attributes.AddRange(GetAttributesForServiceResource(spec.ServiceResource ?? serviceResource));

                if (partyAttribute is not null) attributes.Add(partyAttribute);
                if (spec.ServiceResource is null)
                {
                    attributes.Add(new()
                    {
                        AttributeId = AttributeIdResourceInstance,
                        Value = instanceRef.Value
                    });
                }
                if (spec.AdditionalResourceAttribute is not null)
                {
                    // Layered on top of the effective resource; unlike legacy attributes this never
                    // overrides the resource itself (write-side validation forbids resource references here).
                    attributes.AddRange(GetResourceAttributesForAuthorizationAttribute(spec.AdditionalResourceAttribute));
                }

                break;

            default:
                break;
        }

        return new XacmlJsonCategory
        {
            Id = id,
            Attribute = attributes
        };
    }

    private static List<XacmlJsonAttribute> GetAttributesForServiceResource(string serviceResource)
    {
        var (ns, value, org) = SplitNamespaceAndValue(serviceResource);
        List<XacmlJsonAttribute> attributes = [new() { AttributeId = ns, Value = value }];

        if (org is not null)
        {
            attributes.Add(new XacmlJsonAttribute { AttributeId = AttributeIdOrg, Value = org });
        }

        return attributes;
    }

    private static List<XacmlJsonAttribute> GetResourceAttributesForAuthorizationAttribute(string subResource)
    {
        var result = new List<XacmlJsonAttribute>();
        var (ns, value, org) = SplitNamespaceAndValue(subResource, AttributeIdSubResource);
        result.Add(new XacmlJsonAttribute { AttributeId = ns, Value = value });
        if (org is not null)
        {
            result.Add(new XacmlJsonAttribute { AttributeId = AttributeIdOrg, Value = org });
        }

        return result;
    }

    private static (string, string, string?) SplitNamespaceAndValue(string serviceResource, string defaultNamespace = AttributeIdResource)
    {
        var lastColonIndex = serviceResource.LastIndexOf(':');
        if (lastColonIndex == -1 || lastColonIndex == serviceResource.Length - 1)
        {
            // If we don't recognize the format, we just return the whole string as the value and assume
            // that the caller wants to refer a resource in the Resource Registry namespace.
            return (defaultNamespace, serviceResource, null);
        }

        var ns = serviceResource[..lastColonIndex];
        var value = serviceResource[(lastColonIndex + 1)..];

        if (!value.StartsWith(ReservedResourcePrefixForApps, StringComparison.Ordinal))
        {
            return (ns, value, null);
        }

        // If the value starts with the reserved app prefix, we assume that the value is an app id,
        // and we need to split it into the org and app id based on the format "app_{org}_{app_id}".
        // We also use the app namespace for the attribute id.
        var parts = value.Split('_');
        return parts.Length >= 3
            ? (AttributeIdApp, string.Join('_', parts[2..]), parts[1])
            : (AttributeIdApp, value, null);
    }

    private static XacmlJsonAttribute? GetPartyAttribute(string party)
    {
        var lastColonIndex = party.LastIndexOf(':');
        if (lastColonIndex == -1 || lastColonIndex == party.Length - 1)
        {
            return null;
        }

        return new XacmlJsonAttribute
        {
            AttributeId = party[..lastColonIndex],
            Value = party[(lastColonIndex + 1)..]
        };
    }

    internal static void XacmlRequestRemoveSensitiveInfo(XacmlJsonRequest xacmlJsonRequest)
    {
        var attributes = xacmlJsonRequest
            .GetAllXacmlJsonAttributes()
            .Where(x => x.AttributeId == NorwegianPersonIdentifier.Prefix)
            .ToList();

        foreach (var attr in attributes)
        {
            attr.Value = "Anonymized";
        }
    }

    private static IEnumerable<XacmlJsonAttribute> GetAllXacmlJsonAttributes(this XacmlJsonRequest request)
    {
        return Enumerable.Empty<XacmlJsonAttribute?>()
            .Concat(request.Category.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.Resource.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.Action.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.AccessSubject.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.RecipientSubject.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.IntermediarySubject.EmptyIfNull().SelectMany(category => category.Attribute))
            .Concat(request.RequestingMachine.EmptyIfNull().SelectMany(category => category.Attribute))
            .Where(attribute => attribute is not null)
            .Cast<XacmlJsonAttribute>();
    }

    private static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) => source ?? [];
}
