using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<SearchDialogInput, SearchDialogQuery>()
            .ForMember(dest => dest.OrderBy, opt => opt.Ignore())
            .ForMember(dest => dest.ContinuationToken, opt => opt.Ignore())
            .ForMember(dest => dest.SystemLabel, opt => opt.MapFrom(src => src.SystemLabel))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        CreateMap<PaginatedList<DialogDto>, SearchDialogsPayload>()
            .ForMember(dest => dest.OrderBy, opt => opt.Ignore());

        CreateMap<ContentDto, SearchContent>();

        CreateMap<DialogDto, SearchDialog>();

        CreateMap<DialogEndUserContextDto, EndUserContext>();
    }
}
