using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // See IntermediateSearchDialogDto
        CreateMap<IntermediateDialogDto, DialogDto>()
            .ForMember(dest => dest.LatestActivity, opt => opt.MapFrom(src => src.Activities
                .FirstOrDefault()
            ))
            .ForMember(dest => dest.GuiAttachmentCount, opt => opt.MapFrom(src => src.Attachments
                .Count(x => x.Urls
                    .Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))
            ))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content));

        CreateMap<DialogEntity, IntermediateDialogDto>()
            .ForMember(dest => dest.Activities, opt => opt.MapFrom(src => src.Activities
                .OrderByDescending(activity => activity.CreatedAt).ThenByDescending(activity => activity.Id)
            // Avoid FirstOrDefault here, as this causes EF to generate a large amount of inefficient of SQL
            // Same with Take(1), which results in a very expensive PARTITION OVER query. Works better to handle the final
            // in the IntermediateDialogDto to DialogDto mapping
            ))
            .ForMember(dest => dest.SeenSinceLastUpdate, opt => opt.MapFrom(src => src.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.UpdatedAt)
                .OrderByDescending(x => x.CreatedAt)
            ))
            // Works much better to handle the final count in the IntermediateDialogDto to DialogDto mapping
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments))
            // Not filtering for OutputInList here results in a simpler SQL query that performs better. The DTO only contains the correct fields anyway
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
            .ForMember(dest => dest.SystemLabel, opt => opt.MapFrom(src => src.DialogEndUserContext.SystemLabelId));

        CreateMap<DialogServiceOwnerContext, DialogServiceOwnerContextDto>();
        CreateMap<DialogServiceOwnerLabel, ServiceOwnerLabelDto>();

        CreateMap<DialogSeenLog, DialogSeenLogDto>()
            .ForMember(dest => dest.SeenAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<DialogActivity, DialogActivityDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId));

        CreateMap<List<DialogContent>?, ContentDto?>()
            .ConvertUsing<DialogContentOutputConverter<ContentDto>>();
    }
}
