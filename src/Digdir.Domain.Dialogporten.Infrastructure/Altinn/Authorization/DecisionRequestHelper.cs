using System.Diagnostics;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using System.Security.Claims;

using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class DecisionRequestHelper
{
    private const string SubjectId = "s1";

    private const string PidClaimType = "pid";
    private const string UserIdClaimType = "urn:altinn:userid";
    private const string PartyUuidClaimType = "urn:altinn:party:uuid";
    private const string RarAuthorizationDetailsClaimType = "authorization_details";

    private const string AttributeIdAction = "urn:oasis:names:tc:xacml:1.0:action:action-id";
    private const string AttributeIdResource = "urn:altinn:resource";
    private const string AttributeIdResourceInstance = "urn:altinn:resourceinstance";
    private const string AttributeIdSubResource = "urn:altinn:subresource";

    private const string AttributeIdOrg = "urn:altinn:org";
    private const string AttributeIdApp = "urn:altinn:app";
    private const string AttributeIdAppInstance = "urn:altinn:instance-id";

    private const string AttributeIdUserId = "urn:altinn:userid";
    private const string AttributeIdPerson = "urn:altinn:person:identifier-no";
    private const string AttributeIdSystemUser = "urn:altinn:systemuser:uuid";
    private const string AttributeIdPartyUuid = "urn:altinn:party:uuid";

    // The order of these attribute types is important as we want to prioritize the most specific claim types.
    private static readonly List<string> PrioritizedClaimTypes = [AttributeIdPartyUuid, AttributeIdUserId, AttributeIdPerson, AttributeIdSystemUser];

    private const string ReservedResourcePrefixForApps = "app_";

    private const string PermitResponse = "Permit";

    public static XacmlJsonRequestRoot CreateDialogDetailsRequest(DialogDetailsAuthorizationRequest request)
    {
        var sortedActions = request.AltinnActions.SortForXacml();

        // The PDP does not support self-identified users as parties, so we need to use the party uuid claim instead.
        var party = request.ClaimsPrincipal.GetEndUserPartyIdentifier()
                is AltinnSelfIdentifiedUserIdentifier or IdportenSelfIdentifiedUserIdentifier or FeideUserIdentifier
                    && request.ClaimsPrincipal.TryGetPartyUuid(out var partyUuid)
            ? $"{PartyUuidClaimType}:{partyUuid}"
            : request.Party;

        var accessSubject = CreateAccessSubjectCategory(request.ClaimsPrincipal.Claims);
        var actions = CreateActionCategories(sortedActions, out var actionIdByName);
        var resources = CreateResourceCategories(request.ServiceResource, request.DialogId, party, sortedActions, out var resourceIdByName);

        var multiRequests = CreateMultiRequests(sortedActions, actionIdByName, resourceIdByName);

        var xacmlJsonRequest = new XacmlJsonRequest
        {
            AccessSubject = accessSubject,
            Action = actions,
            Resource = resources,
            MultiRequests = multiRequests
        };

        return new XacmlJsonRequestRoot { Request = xacmlJsonRequest };
    }

    public static DialogDetailsAuthorizationResult CreateDialogDetailsResponse(List<AltinnAction> altinnActions, XacmlJsonResponse? xamlJsonResponse)
    {
        var authorizedAltinnActions = new List<AltinnAction>();

        var sortedAltinnActions = altinnActions.SortForXacml();
        var xacmlJsonResults = xamlJsonResponse?.Response ?? [];

        var count = Math.Min(sortedAltinnActions.Count, xacmlJsonResults.Count);
        for (var i = 0; i < count; i++)
        {
            var action = sortedAltinnActions[i];
            var response = xacmlJsonResults[i];
            if (response.Decision == PermitResponse)
            {
                authorizedAltinnActions.Add(action);
            }
        }

        return new DialogDetailsAuthorizationResult
        {
            AuthorizedAltinnActions = authorizedAltinnActions
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
            PartyUuidClaimType => new XacmlJsonCategory
            {
                Id = SubjectId,
                Attribute = [new() { AttributeId = AttributeIdPartyUuid, Value = claim.Value }]
            },
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

    private static List<XacmlJsonCategory> CreateActionCategories(
        List<AltinnAction> altinnActions, out Dictionary<string, string> actionIdByName)
    {
        actionIdByName = altinnActions
            .Select(x => x.Name)
            .Distinct()
            .Select((action, index) => (action, id: $"a{index + 1}"))
            .ToDictionary(x =>
                x.action,
                x => x.id);

        return actionIdByName
            .Select(x => new XacmlJsonCategory
            {
                Id = x.Value,
                Attribute = [new() { AttributeId = AttributeIdAction, Value = x.Key }]
            })
            .ToList();
    }

    private static List<XacmlJsonCategory> CreateResourceCategories(
        string serviceResource,
        Guid dialogId,
        string party,
        List<AltinnAction> altinnActions, out Dictionary<string, string> resourceIdByName)
    {
        resourceIdByName = altinnActions
            .Select(x => x.AuthorizationAttribute)
            .Distinct()
            .Select((authorizationAttribute, index) => (authorizationAttribute, id: $"r{index + 1}"))
            .ToDictionary(x =>
                x.authorizationAttribute,
                x => x.id);


        var partyAttribute = GetPartyAttribute(party);
        return resourceIdByName
            .Select(x =>
                CreateResourceCategory(x.Value, serviceResource, dialogId, partyAttribute, x.Key))
            .ToList();
    }

    private static XacmlJsonCategory CreateResourceCategory(string id, string serviceResource, Guid? dialogId, XacmlJsonAttribute? partyAttribute, string? authorizationAttribute = null)
    {
        var (ns, value, org) = SplitNamespaceAndValue(serviceResource);
        var attributes = new List<XacmlJsonAttribute>
        {
            new() { AttributeId = ns, Value = value }
        };

        if (org is not null)
        {
            attributes.Add(new XacmlJsonAttribute { AttributeId = AttributeIdOrg, Value = org });
        }

        if (partyAttribute is not null)
        {
            attributes.Add(partyAttribute);
        }

        if (dialogId is not null)
        {
            if (ns == AttributeIdResource)
            {
                attributes.Add(new()
                {
                    AttributeId = AttributeIdResourceInstance,
                    Value = dialogId.ToString()
                });
            }
            else if (ns == AttributeIdAppInstance)
            {
                // TODO!
                // For app instances, we the syntax of the value is "{partyID}/{instanceID}".
                // We do not have Altinn partyID in the request, so we cannot support this.
                // This means we cannot easily support instance specific authorizations for apps.
                // This should probably be fixed in the PDP, lest we use the party lookup service
                // to get this value (which would suck).
                /*
                {
                    AttributeId = AttributeIdAppInstance,
                    Value = dialogId.ToString()
                });
                */
            }
        }

        if (authorizationAttribute is not null)
        {
            var resourceAttributesFromAuthorizationAttribute = GetResourceAttributesForAuthorizationAttribute(authorizationAttribute);

            // If we get either urn:altinn:app/urn:altinn:org or urn:altinn:resource attributes, this should
            // be considered overrides that should be used instead of the default resource attributes.
            if (resourceAttributesFromAuthorizationAttribute.Any(x => x.AttributeId is AttributeIdApp or AttributeIdOrg or AttributeIdResource))
            {
                attributes.RemoveAll(x =>
                    x.AttributeId is AttributeIdResource or AttributeIdResourceInstance or AttributeIdApp or AttributeIdOrg or AttributeIdAppInstance);
            }

            attributes.AddRange(resourceAttributesFromAuthorizationAttribute);
        }

        return new XacmlJsonCategory
        {
            Id = id,
            Attribute = attributes
        };
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
        if (PartyIdentifier.TryParse(party, out var partyIdentifier))
        {
            return new XacmlJsonAttribute
            {
                AttributeId = partyIdentifier.Prefix(),
                Value = partyIdentifier.Id
            };
        }

        return null;
    }

    private static XacmlJsonMultiRequests CreateMultiRequests(
        List<AltinnAction> altinnActions,
        Dictionary<string, string> actionIdByName,
        Dictionary<string, string> resourceIdByName)
    {
        var multiRequests = new XacmlJsonMultiRequests
        {
            RequestReference = []
        };


        foreach (var (actionName, actionId) in actionIdByName)
        {
            foreach (var resourceName in altinnActions.Where(x => x.Name == actionName).Select(x => x.AuthorizationAttribute))
            {
                multiRequests.RequestReference.Add(new XacmlJsonRequestReference
                {
                    ReferenceId = [SubjectId, resourceIdByName[resourceName], actionId]
                });
            }
        }

        return multiRequests;
    }

    private static List<AltinnAction> SortForXacml(this List<AltinnAction> altinnActions) =>
        altinnActions.OrderBy(x => x.Name).ThenBy(x => x.AuthorizationAttribute).ToList();

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
