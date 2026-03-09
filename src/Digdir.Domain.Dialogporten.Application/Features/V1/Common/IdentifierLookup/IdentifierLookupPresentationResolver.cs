using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

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
