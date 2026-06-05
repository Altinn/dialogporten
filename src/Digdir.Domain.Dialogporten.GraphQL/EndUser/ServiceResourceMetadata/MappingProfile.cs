using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<GetServiceResourceMetadataDto, ServiceResourceMetadata>();
        CreateMap<ServiceResourceMetadataItemDto, ServiceResourceMetadataItem>();
        CreateMap<ServiceResourceMetadataServiceResourceDto, ServiceResourceMetadataServiceResource>();
        CreateMap<ServiceResourceMetadataRoleDto, ServiceResourceMetadataRole>();
        CreateMap<ServiceResourceMetadataAccessPackageDto, ServiceResourceMetadataAccessPackage>();
        CreateMap<ServiceResourceMetadataServiceOwnerDto, ServiceResourceMetadataServiceOwner>();
        CreateMap<LinkDto, ServiceResourceMetadataLinks>();
    }
}
