using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata;

internal static class ServiceResourceMetadataMapExtensions
{
    extension(GetServiceResourceMetadataDto source)
    {
        public ServiceResourceMetadata ToServiceResourceMetadata() => new()
        {
            Items = source.Items.Select(MapItem).ToList()
        };
    }

    extension(SearchAuthorizedServiceResourcesDto source)
    {
        public ServiceResourceMetadata ToServiceResourceMetadata() => new()
        {
            Items = source.Items.Select(MapItem).ToList()
        };
    }

    private static ServiceResourceMetadataItem MapItem(ServiceResourceMetadataItemDto item) => new()
    {
        ServiceResource = new ServiceResourceMetadataServiceResource
        {
            Id = item.ServiceResource.Id,
            ResourceType = item.ServiceResource.ResourceType,
            Status = item.ServiceResource.Status,
            IsDelegable = item.ServiceResource.IsDelegable,
            MinimumAuthenticationLevel = item.ServiceResource.MinimumAuthenticationLevel,
            Name = MapLocalizations(item.ServiceResource.Name),
            Links = MapLinks(item.ServiceResource.Links)
        },
        Roles = item.Roles
            .Select(x => new ServiceResourceMetadataRole
            {
                Urn = x.Urn,
                Name = MapLocalizations(x.Name),
                Links = MapLinks(x.Links)
            })
            .ToList(),
        AccessPackages = item.AccessPackages
            .Select(x => new ServiceResourceMetadataAccessPackage
            {
                Urn = x.Urn,
                Name = MapLocalizations(x.Name),
                Links = MapLinks(x.Links)
            })
            .ToList(),
        ServiceOwner = new ServiceResourceMetadataServiceOwner
        {
            OrgNumber = item.ServiceOwner.OrgNumber,
            Code = item.ServiceOwner.Code,
            Name = MapLocalizations(item.ServiceOwner.Name)
        }
    };

    private static List<Localization> MapLocalizations(List<LocalizationDto> source) =>
        source
            .Select(x => new Localization { Value = x.Value, LanguageCode = x.LanguageCode })
            .ToList();

    private static ServiceResourceMetadataLinks MapLinks(LinkDto source) =>
        new() { Metadata = source.Metadata };
}
