using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves localized service resource and service owner presentation values for lookup responses.
/// </summary>
internal sealed class IdentifierLookupPresentationResolver : IIdentifierLookupPresentationResolver
{
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceOwnerNameRegistry _serviceOwnerNameRegistry;

    public IdentifierLookupPresentationResolver(
        IResourceRegistry resourceRegistry,
        IServiceOwnerNameRegistry serviceOwnerNameRegistry)
    {
        ArgumentNullException.ThrowIfNull(resourceRegistry);
        ArgumentNullException.ThrowIfNull(serviceOwnerNameRegistry);

        _resourceRegistry = resourceRegistry;
        _serviceOwnerNameRegistry = serviceOwnerNameRegistry;
    }

    /// <summary>
    /// Fetches presentation metadata and applies fallback and language pruning.
    /// </summary>
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

        if (!string.IsNullOrWhiteSpace(ownerInfo?.ShortName))
        {
            ownerCode = ownerInfo.ShortName;
        }

        var resourceId = StripPrefix(serviceResource);
        var serviceResourceName = ToLocalizationDtos(resourceInformation?.DisplayName, resourceId);
        var serviceOwnerName = ToLocalizationDtos(ownerInfo?.DisplayName, ownerCode);

        serviceResourceName.PruneLocalizations(acceptedLanguages);
        serviceOwnerName.PruneLocalizations(acceptedLanguages);

        return (
            new IdentifierLookupServiceResourceDto
            {
                Id = resourceId,
                Name = serviceResourceName,
                IsDelegable = resourceInformation?.Delegable ?? false
            },
            new IdentifierLookupServiceOwnerDto
            {
                OrgNumber = ownerOrgNumber,
                Code = ownerCode,
                Name = serviceOwnerName
            }
        );
    }

    private static string StripPrefix(string serviceResource)
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
}
