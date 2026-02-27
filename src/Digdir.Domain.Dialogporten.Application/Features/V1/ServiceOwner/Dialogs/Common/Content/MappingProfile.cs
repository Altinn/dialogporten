using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogContent, AuthorizationContentValueDto>()
            .ForMember(x => x.IsAuthorized, opt => opt.Ignore());
    }
}
