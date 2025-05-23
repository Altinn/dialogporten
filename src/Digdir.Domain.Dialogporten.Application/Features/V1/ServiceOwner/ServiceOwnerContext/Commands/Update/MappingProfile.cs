using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ServiceOwnerLabelDto, DialogServiceOwnerLabel>();
    }
}
