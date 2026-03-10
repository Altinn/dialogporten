using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves end-user lookup access flags and authorization evidence for a resolved dialog.
/// </summary>
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

    /// <summary>
    /// Computes authorization evidence and whether lookup access should be granted.
    /// </summary>
    public async Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceRef requestRef,
        string responseInstanceRef,
        CancellationToken cancellationToken)
    {
        var minimumAuthenticationLevel = await _db.ResourcePolicyInformation
            .AsNoTracking()
            .Where(x => x.Resource == dialogData.ServiceResource)
            .Select(x => x.MinimumAuthenticationLevel)
            .FirstOrDefaultAsync(cancellationToken);

        var currentAuthenticationLevel = _user.GetPrincipal().GetAuthenticationLevel();
        var partyIdentifier = _user.GetPrincipal().GetEndUserPartyIdentifier();
        if (partyIdentifier is null)
        {
            return new IdentifierLookupAuthorizationResolution(
                false,
                new IdentifierLookupAuthorizationEvidenceDto
                {
                    MinimumAuthenticationLevel = minimumAuthenticationLevel,
                    CurrentAuthenticationLevel = currentAuthenticationLevel
                });
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
            requestRef.Value,
            responseInstanceRef);

        if (viaInstanceDelegation)
        {
            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = IdentifierLookupGrantType.InstanceDelegation,
                Subject = responseInstanceRef
            });
        }

        var hasAccess = viaRole
                        || viaAccessPackage
                        || viaResourceDelegation
                        || viaInstanceDelegation;

        return new IdentifierLookupAuthorizationResolution(
            hasAccess,
            new IdentifierLookupAuthorizationEvidenceDto
            {
                MinimumAuthenticationLevel = minimumAuthenticationLevel,
                CurrentAuthenticationLevel = currentAuthenticationLevel,
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
        string requestInstanceRef,
        string responseInstanceRef)
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

            var comparableInstanceRef = ToComparableInstanceRef(authorizedInstance);
            if (comparableInstanceRef is null)
            {
                continue;
            }

            if (string.Equals(comparableInstanceRef, requestInstanceRef, StringComparison.OrdinalIgnoreCase)
                || string.Equals(comparableInstanceRef, responseInstanceRef, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ToComparableInstanceRef(AuthorizedResource authorizedInstance)
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

        var suffix = normalized[AppInstanceRefPrefix.Length..];
        var separator = suffix.IndexOf('/');
        if (separator <= 0 || separator == suffix.Length - 1)
        {
            return null;
        }

        return normalized;
    }
}
