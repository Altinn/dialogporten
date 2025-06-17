using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.DialogSystemLabels.Commands.Set;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SetSystemLabelInput, SetSystemLabelCommand>()
            .ForMember(dest => dest.SystemLabels, opt => opt.MapFrom(src => src.SystemLabels));

        CreateMap<DialogRevisionInput, DialogRevisionDto>();

        CreateMap<BulkSetSystemLabelInput, BulkSetSystemLabelDto>();

        CreateMap<BulkSetSystemLabelInput, BulkSetSystemLabelCommand>()
            .ForMember(dest => dest.Dto, opt => opt.MapFrom(src => src));
    }
}
