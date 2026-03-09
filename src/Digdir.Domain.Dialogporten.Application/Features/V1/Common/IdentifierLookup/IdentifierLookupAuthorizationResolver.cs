using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal sealed class IdentifierLookupAuthorizationResolver : IIdentifierLookupAuthorizationResolver
{
    private const string RolePrefix = "urn:altinn:rolecode:";
    private const string AccessPackagePrefix = "urn:altinn:accesspackage:";
    private const string AppInstanceRefPrefix = "urn:altinn:instance-id:";

    private readonly IUser _user;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogDbContext _db;

    public IdentifierLookupAuthorizationResolver(
        IUser user,
        IAltinnAuthorization altinnAuthorization,
        IDialogDbContext db)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceUrn requestUrn,
        string responseInstanceUrn,
        CancellationToken cancellationToken)
    {
        var partyIdentifier = _user.GetPrincipal().GetEndUserPartyIdentifier();
        if (partyIdentifier is null)
        {
            return new IdentifierLookupAuthorizationResolution(
                false,
                new IdentifierLookupAuthorizationEvidenceDto());
        }

        var authorizedParties = await _altinnAuthorization.GetAuthorizedPartiesForLookup(
            partyIdentifier,
            [dialogData.Party],
            cancellationToken);

        var matchingAuthorizedParties = authorizedParties.AuthorizedParties
            .Where(x => string.Equals(x.Party, dialogData.Party, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var authorizedSubjects = matchingAuthorizedParties
            .SelectMany(x => x.AuthorizedRolesAndAccessPackages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var evidenceItems = new List<IdentifierLookupAuthorizationEvidenceItemDto>();

        var roleAndAccessPackageSubjects = await ResolveRoleAndAccessPackageSubjects(
            dialogData.ServiceResource,
            authorizedSubjects,
            cancellationToken);

        foreach (var subject in roleAndAccessPackageSubjects)
        {
            var grantType = subject.StartsWith(RolePrefix, StringComparison.Ordinal)
                ? IdentifierLookupGrantType.Role
                : subject.StartsWith(AccessPackagePrefix, StringComparison.Ordinal)
                    ? IdentifierLookupGrantType.AccessPackage
                    : (IdentifierLookupGrantType?)null;

            if (grantType is null)
            {
                continue;
            }

            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = grantType.Value,
                Subject = subject
            });
        }

        var viaRole = evidenceItems.Any(x => x.GrantType == IdentifierLookupGrantType.Role);
        var viaAccessPackage = evidenceItems.Any(x => x.GrantType == IdentifierLookupGrantType.AccessPackage);

        var viaResourceDelegation = matchingAuthorizedParties
            .SelectMany(x => x.AuthorizedResources)
            .Any(x => string.Equals(x, dialogData.ServiceResource, StringComparison.OrdinalIgnoreCase));

        if (viaResourceDelegation)
        {
            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = IdentifierLookupGrantType.ResourceDelegation,
                Subject = dialogData.ServiceResource
            });
        }

        var viaInstanceDelegation = HasInstanceDelegation(
            matchingAuthorizedParties
                .SelectMany(x => x.AuthorizedInstances)
                .ToList(),
            dialogData.ServiceResource,
            requestUrn.Value,
            responseInstanceUrn);

        if (viaInstanceDelegation)
        {
            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = IdentifierLookupGrantType.InstanceDelegation,
                Subject = responseInstanceUrn
            });
        }

        var hasRequiredAuthLevel = await _altinnAuthorization.UserHasRequiredAuthLevel(
            dialogData.ServiceResource,
            cancellationToken);

        var hasAccess = hasRequiredAuthLevel
                        && (viaRole
                            || viaAccessPackage
                            || viaResourceDelegation
                            || viaInstanceDelegation);

        return new IdentifierLookupAuthorizationResolution(
            hasAccess,
            new IdentifierLookupAuthorizationEvidenceDto
            {
                ViaRole = viaRole,
                ViaAccessPackage = viaAccessPackage,
                ViaResourceDelegation = viaResourceDelegation,
                ViaInstanceDelegation = viaInstanceDelegation,
                Evidence = evidenceItems
            });
    }

    private async Task<List<string>> ResolveRoleAndAccessPackageSubjects(
        string serviceResource,
        List<string> authorizedSubjects,
        CancellationToken cancellationToken)
    {
        if (authorizedSubjects.Count == 0)
        {
            return [];
        }

        var authorizedSubjectsSet = authorizedSubjects.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await _db.SubjectResources
            .AsNoTracking()
            .Where(x => x.Resource == serviceResource && authorizedSubjectsSet.Contains(x.Subject))
            .Select(x => x.Subject)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static bool HasInstanceDelegation(
        List<AuthorizedResource> authorizedInstances,
        string serviceResource,
        string requestInstanceUrn,
        string responseInstanceUrn)
    {
        if (authorizedInstances.Count == 0)
        {
            return false;
        }

        var serviceResourceId = serviceResource.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? serviceResource[Constants.ServiceResourcePrefix.Length..]
            : serviceResource;

        foreach (var authorizedInstance in authorizedInstances)
        {
            if (!string.Equals(authorizedInstance.ResourceId, serviceResourceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var comparableInstanceUrn = ToComparableInstanceUrn(authorizedInstance);
            if (comparableInstanceUrn is null)
            {
                continue;
            }

            if (string.Equals(comparableInstanceUrn, requestInstanceUrn, StringComparison.OrdinalIgnoreCase)
                || string.Equals(comparableInstanceUrn, responseInstanceUrn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ToComparableInstanceUrn(AuthorizedResource authorizedInstance)
    {
        var instanceRef = string.IsNullOrWhiteSpace(authorizedInstance.InstanceRef)
            ? authorizedInstance.InstanceId
            : authorizedInstance.InstanceRef;

        if (string.IsNullOrWhiteSpace(instanceRef))
        {
            return null;
        }

        var normalized = instanceRef.ToLowerInvariant();
        if (normalized.StartsWith(Constants.ServiceContextInstanceIdPrefix, StringComparison.Ordinal))
        {
            normalized = $"{AppInstanceRefPrefix}{normalized[Constants.ServiceContextInstanceIdPrefix.Length..]}";
        }

        if (!normalized.StartsWith(AppInstanceRefPrefix, StringComparison.Ordinal))
        {
            return normalized;
        }

        var separator = normalized.LastIndexOf('/');
        if (separator < 0 || separator == normalized.Length - 1)
        {
            return null;
        }

        return InstanceUrn.AppInstancePrefix + normalized[(separator + 1)..];
    }
}
