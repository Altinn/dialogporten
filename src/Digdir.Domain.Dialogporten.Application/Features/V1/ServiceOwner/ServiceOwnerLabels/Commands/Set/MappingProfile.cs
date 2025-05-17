using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerLabels.Commands.Set;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ServiceOwnerLabelDto, ServiceOwnerLabel>()
            .IgnoreComplexDestinationProperties()
            .ForMember(x => x.Id, opt => opt.Ignore());
    }
}
