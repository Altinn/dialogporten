using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal enum IdentifierLookupDeletedDialogVisibility
{
    ExcludeDeleted = 0,
    IncludeDeleted = 1
}

/// <summary>
/// Resolves dialog data used by identifier lookup responses, keeping lookup-specific data access logic out of handlers.
/// </summary>
internal interface IIdentifierLookupDialogResolver
{
    Task<IdentifierLookupDialogData?> Resolve(
        InstanceUrn urn,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken);

    string ResolveOutputInstanceUrn(InstanceUrn requestUrn, IdentifierLookupDialogData dialogData);
}

/// <summary>
/// Resolves localized service resource and service owner presentation data, including shared fallback and language-pruning behavior.
/// </summary>
internal interface IIdentifierLookupPresentationResolver
{
    Task<(IdentifierLookupServiceResourceDto ServiceResource, IdentifierLookupServiceOwnerDto ServiceOwner)> Resolve(
        string serviceResource,
        string orgCode,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken);
}

/// <summary>
/// Resolves authorization result and evidence for lookup responses.
/// </summary>
internal interface IIdentifierLookupAuthorizationResolver
{
    Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceUrn requestUrn,
        string responseInstanceUrn,
        CancellationToken cancellationToken);
}

internal sealed class IdentifierLookupDialogResolver : IIdentifierLookupDialogResolver
{
    private readonly IDialogDbContext _db;

    public IdentifierLookupDialogResolver(IDialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IdentifierLookupDialogData?> Resolve(
        InstanceUrn urn,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken)
    {
        var dialogs = GetDialogQuery(deletedDialogVisibility);

        var dialogId = urn.Type is InstanceUrnType.DialogId
            ? urn.Id
            : await ResolveDialogIdFromLabel(dialogs, urn.Value, cancellationToken);

        if (dialogId == Guid.Empty)
        {
            return null;
        }

        var projection = await dialogs
            .Where(x => x.Id == dialogId)
            .Select(x => new IdentifierLookupDialogProjection
            {
                DialogId = x.Id,
                Party = x.Party,
                Org = x.Org,
                ServiceResource = x.ServiceResource,
                ServiceOwnerLabels = x.ServiceOwnerContext.ServiceOwnerLabels
                    .Select(l => l.Value)
                    .ToList(),
                Title = x.Content
                    .Where(c => c.TypeId == DialogContentType.Values.Title)
                    .SelectMany(c => c.Value.Localizations
                        .Select(l => new ResourceLocalization(l.LanguageCode, l.Value)))
                    .ToList(),
                NonSensitiveTitle = x.Content
                    .Where(c => c.TypeId == DialogContentType.Values.NonSensitiveTitle)
                    .SelectMany(c => c.Value.Localizations
                        .Select(l => new ResourceLocalization(l.LanguageCode, l.Value)))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        return new IdentifierLookupDialogData(
            projection.DialogId,
            projection.Party,
            projection.Org,
            projection.ServiceResource,
            projection.ServiceOwnerLabels,
            projection.Title,
            projection.NonSensitiveTitle.Count > 0 ? projection.NonSensitiveTitle : null);
    }

    // For a given request URN and resolved dialog data, determine the most appropriate instance URN to return in the response,
    // preferring app instance URNs, then correspondence URNs, then dialog URNs. There should not be multiple app instance URNs
    // or correspondence URNs for a given dialog, but if there are, prefer the one that is last in ordinal descending order (newest).
    public string ResolveOutputInstanceUrn(InstanceUrn requestUrn, IdentifierLookupDialogData dialogData)
    {
        if (requestUrn.Type is not InstanceUrnType.DialogId)
        {
            return requestUrn.Value;
        }

        var appInstanceUrn = dialogData.ServiceOwnerLabels
            .Where(x => x.StartsWith(InstanceUrn.AppInstancePrefix, StringComparison.Ordinal))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (appInstanceUrn is not null)
        {
            return appInstanceUrn;
        }

        var correspondenceUrn = dialogData.ServiceOwnerLabels
            .Where(x => x.StartsWith(InstanceUrn.CorrespondencePrefix, StringComparison.Ordinal))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (correspondenceUrn is not null)
        {
            return correspondenceUrn;
        }

        return InstanceUrn.CreateDialogUrn(dialogData.DialogId).ToLowerInvariant();
    }

    private IQueryable<DialogEntity> GetDialogQuery(
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility)
    {
        var dialogs = _db.Dialogs.AsNoTracking();
        if (deletedDialogVisibility is IdentifierLookupDeletedDialogVisibility.IncludeDeleted)
        {
            dialogs = dialogs.IgnoreQueryFilters();
        }

        return dialogs;
    }

    private static async Task<Guid> ResolveDialogIdFromLabel(
        IQueryable<DialogEntity> dialogs,
        string labelValue,
        CancellationToken cancellationToken) =>
        await dialogs
            .Where(x => x.ServiceOwnerContext.ServiceOwnerLabels.Any(l => l.Value == labelValue))
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private sealed class IdentifierLookupDialogProjection
    {
        public Guid DialogId { get; init; }
        public string Party { get; init; } = null!;
        public string Org { get; init; } = null!;
        public string ServiceResource { get; init; } = null!;
        public List<string> ServiceOwnerLabels { get; init; } = [];
        public List<ResourceLocalization> Title { get; init; } = [];
        public List<ResourceLocalization> NonSensitiveTitle { get; init; } = [];
    }
}

internal sealed class IdentifierLookupPresentationResolver : IIdentifierLookupPresentationResolver
{
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceOwnerNameRegistry _serviceOwnerNameRegistry;

    public IdentifierLookupPresentationResolver(
        IResourceRegistry resourceRegistry,
        IServiceOwnerNameRegistry serviceOwnerNameRegistry)
    {
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _serviceOwnerNameRegistry = serviceOwnerNameRegistry ?? throw new ArgumentNullException(nameof(serviceOwnerNameRegistry));
    }

    public async Task<(IdentifierLookupServiceResourceDto ServiceResource, IdentifierLookupServiceOwnerDto ServiceOwner)> Resolve(
        string serviceResource,
        string orgCode,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken)
    {
        var resourceInformation = await _resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);

        var ownerOrgNumber = resourceInformation?.OwnerOrgNumber ?? string.Empty;
        var ownerCode = resourceInformation?.OwnOrgShortName ?? orgCode;

        var ownerInfo = string.IsNullOrWhiteSpace(ownerOrgNumber)
            ? null
            : await _serviceOwnerNameRegistry.GetServiceOwnerInfo(ownerOrgNumber, cancellationToken);

        if (ownerInfo is not null)
        {
            ownerCode = ownerInfo.ShortName;
        }

        var serviceResourceName = ToLocalizationDtos(resourceInformation?.DisplayName, serviceResource);
        var serviceOwnerName = ToLocalizationDtos(ownerInfo?.DisplayName, ownerCode);

        serviceResourceName.PruneLocalizations(acceptedLanguages);
        serviceOwnerName.PruneLocalizations(acceptedLanguages);

        return (
            new IdentifierLookupServiceResourceDto
            {
                Id = serviceResource,
                Name = serviceResourceName
            },
            new IdentifierLookupServiceOwnerDto
            {
                OrgNumber = ownerOrgNumber,
                Code = ownerCode,
                Name = serviceOwnerName
            }
        );
    }

    private static List<LocalizationDto> ToLocalizationDtos(
        List<ResourceLocalization>? values,
        string fallback)
    {
        var localizations = values
            ?.Where(x => !string.IsNullOrWhiteSpace(x.LanguageCode) && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new LocalizationDto
            {
                LanguageCode = x.LanguageCode,
                Value = x.Value
            })
            .ToList() ?? [];

        return localizations.Count > 0
            ? localizations
            :
            [
                new LocalizationDto
                {
                    LanguageCode = "nb",
                    Value = fallback
                }
            ];
    }
}

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

internal sealed record IdentifierLookupDialogData(
    Guid DialogId,
    string Party,
    string Org,
    string ServiceResource,
    List<string> ServiceOwnerLabels,
    List<ResourceLocalization> Title,
    List<ResourceLocalization>? NonSensitiveTitle);

internal sealed record IdentifierLookupAuthorizationResolution(
    bool HasAccess,
    IdentifierLookupAuthorizationEvidenceDto Evidence);
