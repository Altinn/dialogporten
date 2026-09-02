using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

public interface IServiceResourceMetadataItemBuilder
{
    /// <summary>
    /// Builds enriched service resource metadata items for the given (full URN) resources. The items are not
    /// ordered here; display ordering (and per-request language pruning) is applied by the query handlers via
    /// <c>ServiceResourceMetadataPruneExtensions.ToSortedPrunedItems</c>. Pass <paramref name="acceptedLanguages"/>
    /// = null to build the full, all-language items (the form cached by
    /// <see cref="IServiceResourceMetadataCatalogue"/>); pruning copies, so the shared cached items are never mutated.
    /// </summary>
    Task<List<ServiceResourceMetadataItemDto>> BuildItems(
        IReadOnlyCollection<string> serviceResources,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken ct);
}

internal sealed class ServiceResourceMetadataItemBuilder : IServiceResourceMetadataItemBuilder
{
    private readonly ISubjectResourceRepository _subjectResourceRepository;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceOwnerNameRegistry _serviceOwnerNameRegistry;
    private readonly IServiceResourceMinimumAuthenticationLevelResolver _minimumAuthenticationLevelResolver;
    private readonly IAccessManagementMetadata _accessManagementMetadata;
    private readonly IMetadataLinkProvider _metadataLinkProvider;

    public ServiceResourceMetadataItemBuilder(
        ISubjectResourceRepository subjectResourceRepository,
        IResourceRegistry resourceRegistry,
        IServiceOwnerNameRegistry serviceOwnerNameRegistry,
        IServiceResourceMinimumAuthenticationLevelResolver minimumAuthenticationLevelResolver,
        IAccessManagementMetadata accessManagementMetadata,
        IMetadataLinkProvider metadataLinkProvider)
    {
        ArgumentNullException.ThrowIfNull(subjectResourceRepository);
        ArgumentNullException.ThrowIfNull(resourceRegistry);
        ArgumentNullException.ThrowIfNull(serviceOwnerNameRegistry);
        ArgumentNullException.ThrowIfNull(minimumAuthenticationLevelResolver);
        ArgumentNullException.ThrowIfNull(accessManagementMetadata);
        ArgumentNullException.ThrowIfNull(metadataLinkProvider);

        _subjectResourceRepository = subjectResourceRepository;
        _resourceRegistry = resourceRegistry;
        _serviceOwnerNameRegistry = serviceOwnerNameRegistry;
        _minimumAuthenticationLevelResolver = minimumAuthenticationLevelResolver;
        _accessManagementMetadata = accessManagementMetadata;
        _metadataLinkProvider = metadataLinkProvider;
    }

    public async Task<List<ServiceResourceMetadataItemDto>> BuildItems(
        IReadOnlyCollection<string> serviceResources,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken ct)
    {
        if (serviceResources.Count == 0)
        {
            return [];
        }

        // These are awaited sequentially, not via Task.WhenAll: several of these dependencies resolve over
        // the same scoped DbContext, which is not thread-safe. Running them concurrently throws
        // "A second operation was started on this context instance". The underlying calls are cached, so
        // the sequential cost is negligible.
        var subjectsByResource = await _subjectResourceRepository.GetSubjectsForReferencedPartyResources(ct);
        var rolesAndAccessPackages = await _accessManagementMetadata.GetMetadata(ct);
        var resourceInformationByResource = await _resourceRegistry.GetResourceInformation(serviceResources, ct);
        var minimumAuthenticationLevels = await _minimumAuthenticationLevelResolver.GetMinimumAuthenticationLevels(
            serviceResources,
            ct
        );

        var ownerOrgNumbers = resourceInformationByResource.Values
            .Select(x => x.OwnerOrgNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var ownerInfoByOrgNumber = await _serviceOwnerNameRegistry.GetServiceOwnerInfo(ownerOrgNumbers, ct);

        return serviceResources
            .Select(resource => CreateItem(
                resource,
                subjectsByResource,
                rolesAndAccessPackages,
                resourceInformationByResource,
                ownerInfoByOrgNumber,
                minimumAuthenticationLevels,
                acceptedLanguages))
            // No ordering applied here: BuildItems feeds the cached catalogue, and the query handlers order per
            // request (by the pruned, requested-language name) via ToSortedPrunedItems, so any order set here
            // would just be discarded.
            .ToList();
    }

    private ServiceResourceMetadataItemDto CreateItem(
        string serviceResource,
        IReadOnlyDictionary<string, IReadOnlyList<string>> subjectsByResource,
        AccessManagementMetadata metadata,
        IReadOnlyDictionary<string, ServiceResourceInformation> resourceInformationByResource,
        IReadOnlyDictionary<string, ServiceOwnerInfo> ownerInfoByOrgNumber,
        IReadOnlyDictionary<string, int> minimumAuthenticationLevels,
        List<AcceptedLanguage>? acceptedLanguages)
    {
        resourceInformationByResource.TryGetValue(serviceResource, out var resourceInformation);
        var ownerOrgNumber = resourceInformation?.OwnerOrgNumber ?? string.Empty;
        var ownerCode = resourceInformation?.OwnOrgShortName ?? string.Empty;
        ownerInfoByOrgNumber.TryGetValue(ownerOrgNumber, out var ownerInfo);

        if (!string.IsNullOrWhiteSpace(ownerInfo?.ShortName))
        {
            ownerCode = ownerInfo.ShortName;
        }

        var resourceId = StripResourcePrefix(serviceResource);
        var serviceResourceName = ToLocalizationDtos(resourceInformation?.DisplayName, resourceId);
        var serviceOwnerName = ToLocalizationDtos(ownerInfo?.DisplayName, ownerCode);
        serviceResourceName.PruneLocalizations(acceptedLanguages);
        serviceOwnerName.PruneLocalizations(acceptedLanguages);

        var subjects = subjectsByResource.GetValueOrDefault(serviceResource) ?? [];
        var roles = subjects
            .Where(x => x.StartsWith(AltinnAuthorizationConstants.RolePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => metadata.RolesBySubject.GetValueOrDefault(x))
            .OfType<AccessManagementRoleMetadata>()
            .DistinctBy(x => x.Urn, StringComparer.OrdinalIgnoreCase)
            // Roles/access packages are (re)ordered per request by PrunedCopy, so no ordering here.
            .Select(x => new ServiceResourceMetadataRoleDto
            {
                Urn = x.Urn,
                Name = x.Name.Pruned(acceptedLanguages),
                Links = x.Links
            })
            .ToList();

        var accessPackages = subjects
            .Where(x => x.StartsWith(AltinnAuthorizationConstants.AccessPackagePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => metadata.AccessPackagesBySubject.GetValueOrDefault(x))
            .OfType<AccessManagementAccessPackageMetadata>()
            .DistinctBy(x => x.Urn, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ServiceResourceMetadataAccessPackageDto
            {
                Urn = x.Urn,
                Name = x.Name.Pruned(acceptedLanguages),
                Links = x.Links
            })
            .ToList();

        return new ServiceResourceMetadataItemDto
        {
            ServiceResource = new ServiceResourceMetadataServiceResourceDto
            {
                Id = resourceId,
                ResourceType = resourceInformation?.ResourceType ?? string.Empty,
                Status = resourceInformation?.Status ?? string.Empty,
                IsDelegable = resourceInformation?.Delegable ?? false,
                MinimumAuthenticationLevel = minimumAuthenticationLevels.GetValueOrDefault(serviceResource),
                Name = serviceResourceName,
                Links = new LinkDto { Metadata = _metadataLinkProvider.GetServiceResourceMetadataLink(resourceId) }
            },
            Roles = roles,
            AccessPackages = accessPackages,
            ServiceOwner = new ServiceResourceMetadataServiceOwnerDto
            {
                OrgNumber = ownerOrgNumber,
                Code = ownerCode,
                Name = serviceOwnerName
            }
        };
    }

    private static string StripResourcePrefix(string serviceResource)
        => serviceResource.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? serviceResource[Constants.ServiceResourcePrefix.Length..]
            : serviceResource;

    private static List<LocalizationDto> ToLocalizationDtos(
        IReadOnlyList<ResourceLocalization>? values,
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
