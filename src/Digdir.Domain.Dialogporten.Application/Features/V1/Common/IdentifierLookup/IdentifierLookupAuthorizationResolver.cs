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

        var listAuthorization = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            [dialogData.Party],
            [dialogData.ServiceResource],
            cancellationToken);

        var authorizedSubjects = listAuthorization.SubjectsByParties
            .TryGetValue(dialogData.Party, out var subjects)
            ? subjects.ToList()
            : new List<string>();

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

        var hasDialogIdAuthorization = listAuthorization.DialogIds.Contains(dialogData.DialogId);
        var hasResourceAuthorization = listAuthorization.ResourcesByParties
            .TryGetValue(dialogData.Party, out var resources)
            && resources.Any(x => string.Equals(x, dialogData.ServiceResource, StringComparison.OrdinalIgnoreCase));
        var hasListAuthorization = hasDialogIdAuthorization || hasResourceAuthorization;

        var viaResourceDelegation = hasResourceAuthorization;

        if (viaResourceDelegation)
        {
            evidenceItems.Add(new IdentifierLookupAuthorizationEvidenceItemDto
            {
                GrantType = IdentifierLookupGrantType.ResourceDelegation,
                Subject = dialogData.ServiceResource
            });
        }

        var viaInstanceDelegation = hasDialogIdAuthorization
                                    || HasInstanceDelegation(
                                        listAuthorization,
                                        dialogData.Party,
                                        dialogData.ServiceResource,
                                        requestUrn,
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

        var hasEvidenceAccess = viaRole
                                || viaAccessPackage
                                || viaResourceDelegation
                                || viaInstanceDelegation;

        var hasAccess = hasRequiredAuthLevel && (hasListAuthorization || hasEvidenceAccess);

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
        DialogSearchAuthorizationResult listAuthorization,
        string party,
        string serviceResource,
        InstanceUrn requestUrn,
        string responseInstanceUrn)
    {
        if (!listAuthorization.AuthorizedInstancesByParties.TryGetValue(party, out var authorizedInstances)
            || authorizedInstances.Count == 0)
        {
            return false;
        }

        var responseUrnParsed = InstanceUrn.TryParse(responseInstanceUrn, out var responseUrn)
            ? responseUrn
            : (InstanceUrn?)null;
        var responseId = responseUrnParsed?.Id;

        var serviceResourceId = serviceResource.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? serviceResource[Constants.ServiceResourcePrefix.Length..]
            : serviceResource;

        foreach (var authorizedInstance in authorizedInstances)
        {
            if (!string.Equals(authorizedInstance.ResourceId, serviceResourceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(authorizedInstance.InstanceId, requestUrn.Value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(authorizedInstance.InstanceId, responseInstanceUrn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Guid.TryParse(authorizedInstance.InstanceId, out var instanceGuid)
                && (instanceGuid == requestUrn.Id || responseId == instanceGuid))
            {
                return true;
            }

            if (TryParseStorageInstanceId(authorizedInstance.InstanceId, out instanceGuid)
                && (instanceGuid == requestUrn.Id || responseId == instanceGuid))
            {
                return true;
            }

            if (InstanceUrn.TryParse(authorizedInstance.InstanceId, out var authorizedInstanceUrn)
                && (authorizedInstanceUrn == requestUrn || (responseUrnParsed is { } parsed && authorizedInstanceUrn == parsed)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseStorageInstanceId(string value, out Guid instanceId)
    {
        instanceId = Guid.Empty;

        if (!value.StartsWith(Constants.ServiceContextInstanceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var slashIndex = value.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex == value.Length - 1)
        {
            return false;
        }

        return Guid.TryParse(value[(slashIndex + 1)..], out instanceId);
    }
}
