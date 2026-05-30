using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;

public sealed class GetServiceResourceMetadataQuery : IRequest<GetServiceResourceMetadataDto>, IFeatureMetricServiceResourceIgnoreRequest
{
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

internal sealed class GetServiceResourceMetadataQueryHandler : IRequestHandler<GetServiceResourceMetadataQuery, GetServiceResourceMetadataDto>
{
    private readonly IPartyResourceReferenceRepository _partyResourceReferenceRepository;
    private readonly ISubjectResourceRepository _subjectResourceRepository;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceOwnerNameRegistry _serviceOwnerNameRegistry;
    private readonly IServiceResourceMinimumAuthenticationLevelResolver _minimumAuthenticationLevelResolver;
    private readonly IAccessManagementMetadata _accessManagementMetadata;
    private readonly IMetadataLinkProvider _metadataLinkProvider;

    public GetServiceResourceMetadataQueryHandler(
        IPartyResourceReferenceRepository partyResourceReferenceRepository,
        ISubjectResourceRepository subjectResourceRepository,
        IResourceRegistry resourceRegistry,
        IServiceOwnerNameRegistry serviceOwnerNameRegistry,
        IServiceResourceMinimumAuthenticationLevelResolver minimumAuthenticationLevelResolver,
        IAccessManagementMetadata accessManagementMetadata,
        IMetadataLinkProvider metadataLinkProvider)
    {
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);
        ArgumentNullException.ThrowIfNull(subjectResourceRepository);
        ArgumentNullException.ThrowIfNull(resourceRegistry);
        ArgumentNullException.ThrowIfNull(serviceOwnerNameRegistry);
        ArgumentNullException.ThrowIfNull(minimumAuthenticationLevelResolver);
        ArgumentNullException.ThrowIfNull(accessManagementMetadata);
        ArgumentNullException.ThrowIfNull(metadataLinkProvider);

        _partyResourceReferenceRepository = partyResourceReferenceRepository;
        _subjectResourceRepository = subjectResourceRepository;
        _resourceRegistry = resourceRegistry;
        _serviceOwnerNameRegistry = serviceOwnerNameRegistry;
        _minimumAuthenticationLevelResolver = minimumAuthenticationLevelResolver;
        _accessManagementMetadata = accessManagementMetadata;
        _metadataLinkProvider = metadataLinkProvider;
    }

    public async Task<GetServiceResourceMetadataDto> Handle(
        GetServiceResourceMetadataQuery request,
        CancellationToken cancellationToken)
    {
        var resources = await _partyResourceReferenceRepository.GetReferencedResources(cancellationToken);
        var subjectsByResource = await _subjectResourceRepository.GetSubjectsForReferencedPartyResources(cancellationToken);
        var metadata = await _accessManagementMetadata.GetMetadata(cancellationToken);
        var resourceInformationByResource = await _resourceRegistry.GetResourceInformation(resources, cancellationToken);
        var minimumAuthenticationLevels = await _minimumAuthenticationLevelResolver
            .GetMinimumAuthenticationLevels(resources, cancellationToken);
        var ownerOrgNumbers = resourceInformationByResource.Values
            .Select(x => x.OwnerOrgNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ownerInfoByOrgNumber = await _serviceOwnerNameRegistry.GetServiceOwnerInfo(ownerOrgNumbers, cancellationToken);

        var items = resources
            .Select(resource => CreateItem(
                resource,
                subjectsByResource,
                metadata,
                resourceInformationByResource,
                ownerInfoByOrgNumber,
                minimumAuthenticationLevels,
                request.AcceptedLanguages));

        return new GetServiceResourceMetadataDto
        {
            Items = items
                .OrderBy(x => GetSortName(x.ServiceResource.Name), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.ServiceResource.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private ServiceResourceMetadataItemDto CreateItem(
        string serviceResource,
        Dictionary<string, List<string>> subjectsByResource,
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
            .Select(x => new
            {
                x.Urn,
                Name = PruneLocalizations(x.Name, acceptedLanguages),
                x.Links
            })
            .OrderBy(x => GetSortName(x.Name), StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new ServiceResourceMetadataRoleDto
            {
                Urn = x.Urn,
                Name = x.Name,
                Links = x.Links
            })
            .ToList();

        var accessPackages = subjects
            .Where(x => x.StartsWith(AltinnAuthorizationConstants.AccessPackagePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => metadata.AccessPackagesBySubject.GetValueOrDefault(x))
            .OfType<AccessManagementAccessPackageMetadata>()
            .DistinctBy(x => x.Urn, StringComparer.OrdinalIgnoreCase)
            .Select(x => new
            {
                x.Urn,
                Name = PruneLocalizations(x.Name, acceptedLanguages),
                x.Links
            })
            .OrderBy(x => GetSortName(x.Name), StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new ServiceResourceMetadataAccessPackageDto
            {
                Urn = x.Urn,
                Name = x.Name,
                Links = x.Links
            })
            .ToList();

        return new ServiceResourceMetadataItemDto
        {
            ServiceResource = new ServiceResourceMetadataServiceResourceDto
            {
                Id = resourceId,
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
        => serviceResource.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? serviceResource[Domain.Common.Constants.ServiceResourcePrefix.Length..]
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

    private static List<LocalizationDto> PruneLocalizations(
        IReadOnlyList<LocalizationDto> localizations,
        List<AcceptedLanguage>? acceptedLanguages)
    {
        var prunedLocalizations = localizations.ToList();
        prunedLocalizations.PruneLocalizations(acceptedLanguages);
        return prunedLocalizations;
    }

    private static string GetSortName(List<LocalizationDto> localizations) =>
        localizations.FirstOrDefault(x => x.LanguageCode is "nb")?.Value
        ?? localizations.FirstOrDefault(x => x.LanguageCode is "en")?.Value
        ?? localizations.FirstOrDefault()?.Value
        ?? string.Empty;
}
