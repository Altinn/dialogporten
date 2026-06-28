using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

public static class ServiceResourceMetadataPruneExtensions
{
    /// <summary>
    /// Produces a fresh <see cref="ServiceResourceMetadataItemDto"/> with its localized names pruned to
    /// <paramref name="acceptedLanguages"/>. New DTO instances and new <c>Name</c> lists are allocated (via the
    /// copying <see cref="LocalizationExtensions.Pruned"/>), so the source item — which is a shared, cached,
    /// all-language catalogue entry — is never mutated. Immutable scalar/link data is shared by reference.
    /// Passing null keeps all languages. Roles and access packages are sorted by their pruned name via
    /// <see cref="GetSortName"/>, so their ordering reflects the requested language; top-level item ordering is
    /// the caller's responsibility.
    /// </summary>
    public static ServiceResourceMetadataItemDto PrunedCopy(
        this ServiceResourceMetadataItemDto source,
        List<AcceptedLanguage>? acceptedLanguages) => new()
        {
            ServiceResource = new ServiceResourceMetadataServiceResourceDto
            {
                Id = source.ServiceResource.Id,
                ResourceType = source.ServiceResource.ResourceType,
                Status = source.ServiceResource.Status,
                IsDelegable = source.ServiceResource.IsDelegable,
                MinimumAuthenticationLevel = source.ServiceResource.MinimumAuthenticationLevel,
                Name = source.ServiceResource.Name.Pruned(acceptedLanguages),
                Links = source.ServiceResource.Links
            },
            Roles = source.Roles
                .Select(role => new ServiceResourceMetadataRoleDto
                {
                    Urn = role.Urn,
                    Name = role.Name.Pruned(acceptedLanguages),
                    Links = role.Links
                })
                .OrderBy(role => GetSortName(role.Name), StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            AccessPackages = source.AccessPackages
                .Select(accessPackage => new ServiceResourceMetadataAccessPackageDto
                {
                    Urn = accessPackage.Urn,
                    Name = accessPackage.Name.Pruned(acceptedLanguages),
                    Links = accessPackage.Links
                })
                .OrderBy(accessPackage => GetSortName(accessPackage.Name), StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            ServiceOwner = new ServiceResourceMetadataServiceOwnerDto
            {
                OrgNumber = source.ServiceOwner.OrgNumber,
                Code = source.ServiceOwner.Code,
                Name = source.ServiceOwner.Name.Pruned(acceptedLanguages)
            }
        };

    /// <summary>
    /// The display name to sort a localized name list by: prefers nb, then en, then the first entry, then empty.
    /// Used both when building the all-language catalogue and when re-sorting per-request pruned copies, so the
    /// two stay consistent.
    /// </summary>
    public static string GetSortName(List<LocalizationDto> localizations) =>
        localizations.FirstOrDefault(x => x.LanguageCode is "nb")?.Value
        ?? localizations.FirstOrDefault(x => x.LanguageCode is "en")?.Value
        ?? localizations.FirstOrDefault()?.Value
        ?? string.Empty;

    /// <summary>
    /// Prunes each catalogue entry's item to <paramref name="acceptedLanguages"/> (fresh per-request copies, so
    /// the shared cached entries are never mutated) and orders the result by the pruned (requested-language)
    /// name, then id. Shared by the public-catalogue and authorized-resources query handlers so the prune+sort
    /// is defined once: the catalogue is built with all languages in no particular display order, so the ordering
    /// a caller sees is established here, against the requested language.
    /// </summary>
    public static List<ServiceResourceMetadataItemDto> ToSortedPrunedItems(
        this IEnumerable<ServiceResourceMetadataCatalogueEntry> entries,
        List<AcceptedLanguage>? acceptedLanguages) =>
        entries
            .Select(entry => entry.Item.PrunedCopy(acceptedLanguages))
            .OrderBy(item => GetSortName(item.ServiceResource.Name), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.ServiceResource.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
