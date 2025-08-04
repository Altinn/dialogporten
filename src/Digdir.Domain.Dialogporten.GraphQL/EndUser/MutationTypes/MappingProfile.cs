using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SetSystemLabelInput, SetSystemLabelCommand>();

        CreateMap<DialogRevisionInput, DialogRevisionDto>();

        CreateMap<BulkSetSystemLabelInput, BulkSetSystemLabelDto>();

        CreateMap<BulkSetSystemLabelInput, BulkSetSystemLabelCommand>()
            .ForMember(dest => dest.Dto, opt => opt.MapFrom(src => src));
    }
}
