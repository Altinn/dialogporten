using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.Set;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SetSystemLabelInput, SetSystemLabelCommand>()
            .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => src.SystemLabels));
    }
}
