using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves end-user lookup access flags and authorization evidence for a resolved dialog.
/// </summary>
internal sealed class IdentifierLookupAuthorizationResolver : IIdentifierLookupAuthorizationResolver
{
    private readonly IUser _user;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogDbContext _db;
    private readonly IAccessManagementMetadata _accessManagementMetadata;

    public IdentifierLookupAuthorizationResolver(
        IUser user,
        IAltinnAuthorization altinnAuthorization,
        IDialogDbContext db,
        IAccessManagementMetadata accessManagementMetadata)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(accessManagementMetadata);

        _user = user;
        _altinnAuthorization = altinnAuthorization;
        _db = db;
        _accessManagementMetadata = accessManagementMetadata;
    }

    /// <summary>
    /// Computes authorization evidence and whether lookup access should be granted.
    /// </summary>
    public async Task<IdentifierLookupAuthorizationResolution> Resolve(IdentifierLookupDialogData dialogData,
        InstanceRef responseInstanceRef,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken)
    {
        var currentAuthenticationLevel = _user.GetPrincipal().GetAuthenticationLevel();
        var partyIdentifier = _user.GetPrincipal().GetEndUserPartyIdentifier();
        if (partyIdentifier is null)
        {
            return CreateUnauthorizedResolution();
        }

        var authorizedParties = await _altinnAuthorization.GetAuthorizedPartiesForLookup(
            partyIdentifier,
            [dialogData.Party],
            cancellationToken);

        var matchingAuthorizedParty = authorizedParties.AuthorizedParties
            .FirstOrDefault(x => string.Equals(x.Party, dialogData.Party, StringComparison.OrdinalIgnoreCase));

        if (matchingAuthorizedParty is null)
        {
            return CreateUnauthorizedResolution();
        }

        var authorizedSubjects = matchingAuthorizedParty.AuthorizedRolesAndAccessPackages
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var evidenceItems = new List<IdentifierLookupAuthorizationEvidenceItemDto>();

        var roleAndAccessPackageSubjects = await ResolveRoleAndAccessPackageSubjects(
            dialogData.ServiceResource,
            authorizedSubjects,
            cancellationToken);

        var metadata = await _accessManagementMetadata.GetMetadata(cancellationToken);

        foreach (var subject in roleAndAccessPackageSubjects)
        {
            var grantType = ResolveGrantType(subject);

            if (grantType is null)
            {
                continue;
            }

            var evidenceItem = new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = grantType.Value,
                Subject = subject
            };

            EnrichEvidenceItem(evidenceItem, metadata, acceptedLanguages);
            evidenceItems.Add(evidenceItem);
        }

        var viaRole = evidenceItems.Any(x => x.GrantType == IdentifierLookupGrantType.Role);
        var viaAccessPackage = evidenceItems.Any(x => x.GrantType == IdentifierLookupGrantType.AccessPackage);

        var viaResourceDelegation = matchingAuthorizedParty.AuthorizedResources
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
            matchingAuthorizedParty.AuthorizedInstances,
            dialogData.ServiceResource,
            responseInstanceRef);

        if (viaInstanceDelegation)
        {
            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = IdentifierLookupGrantType.InstanceDelegation,
                Subject = responseInstanceRef.Value
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
                CurrentAuthenticationLevel = currentAuthenticationLevel,
                ViaRole = viaRole,
                ViaAccessPackage = viaAccessPackage,
                ViaResourceDelegation = viaResourceDelegation,
                ViaInstanceDelegation = viaInstanceDelegation,
                Evidence = evidenceItems
            });
    }

    private static IdentifierLookupAuthorizationResolution CreateUnauthorizedResolution() =>
        new(
            false,
            new IdentifierLookupAuthorizationEvidenceDto());

    private static IdentifierLookupGrantType? ResolveGrantType(string subject)
    {
        if (subject.StartsWith(AltinnAuthorizationConstants.RolePrefix, StringComparison.Ordinal))
        {
            return IdentifierLookupGrantType.Role;
        }

        if (subject.StartsWith(AltinnAuthorizationConstants.AccessPackagePrefix, StringComparison.Ordinal))
        {
            return IdentifierLookupGrantType.AccessPackage;
        }

        return null;
    }

    private static void EnrichEvidenceItem(
        IdentifierLookupAuthorizationEvidenceItemDto evidenceItem,
        AccessManagementMetadata metadata,
        List<AcceptedLanguage>? acceptedLanguages)
    {
        switch (evidenceItem.GrantType)
        {
            case IdentifierLookupGrantType.Role:
                if (metadata.RolesBySubject.TryGetValue(evidenceItem.Subject, out var role))
                {
                    evidenceItem.Name = role.Name.Pruned(acceptedLanguages);
                    evidenceItem.Links = role.Links;
                }
                break;
            case IdentifierLookupGrantType.AccessPackage:
                if (metadata.AccessPackagesBySubject.TryGetValue(evidenceItem.Subject, out var accessPackage))
                {
                    evidenceItem.Name = accessPackage.Name.Pruned(acceptedLanguages);
                    evidenceItem.Links = accessPackage.Links;
                }
                break;
            case IdentifierLookupGrantType.ResourceDelegation:
            case IdentifierLookupGrantType.InstanceDelegation:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(evidenceItem), evidenceItem.GrantType, "Unknown grant type.");
        }
    }

    private async Task<List<string>> ResolveRoleAndAccessPackageSubjects(
        string serviceResource,
        List<string> authorizedSubjects,
        CancellationToken cancellationToken) =>
        authorizedSubjects.Count == 0
            ? []
            : await _db.SubjectResources
                .AsNoTracking()
                .Where(x => x.Resource == serviceResource && authorizedSubjects.Contains(x.Subject))
                .Select(x => x.Subject)
                .Distinct()
                .ToListAsync(cancellationToken);

    private static bool HasInstanceDelegation(
        List<AuthorizedResource> authorizedInstances,
        string serviceResource,
        InstanceRef responseInstanceRef)
    {
        if (authorizedInstances.Count == 0)
        {
            return false;
        }

        var serviceResourceId = serviceResource.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? serviceResource[Domain.Common.Constants.ServiceResourcePrefix.Length..]
            : serviceResource;

        foreach (var authorizedInstance in authorizedInstances)
        {
            if (!string.Equals(authorizedInstance.ResourceId, serviceResourceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(authorizedInstance.InstanceRef))
            {
                continue;
            }

            if (string.Equals(authorizedInstance.InstanceRef, responseInstanceRef.Value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
