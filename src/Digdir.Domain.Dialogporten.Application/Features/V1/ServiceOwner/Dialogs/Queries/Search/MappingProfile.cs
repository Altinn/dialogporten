using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // This mapping profile is designed in two stages for performance reasons.
        // 1. DialogEntity -> IntermediateDialogDto: This stage is executed by Entity Framework
        //    and translated to SQL. It's kept simple to generate efficient queries.
        // 2. IntermediateDialogDto -> DialogDto: This stage is executed in-memory after
        //    the data has been fetched from the database. It handles more complex logic.

        // Stage 2: In-memory mapping from intermediate to final DTO
        CreateMap<IntermediateDialogDto, DialogDto>()
            .ForMember(dest => dest.LatestActivity, opt => opt.MapFrom(src => src.Activities.FirstOrDefault()))
            .ForMember(dest => dest.GuiAttachmentCount, opt => opt.MapFrom(src => src.Attachments
                .Count(x => x.Urls.Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))));

        // Stage 1: EF Core to SQL mapping from domain entity to intermediate DTO
        CreateMap<DialogEntity, IntermediateDialogDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
            .ForMember(dest => dest.SystemLabel, opt => opt.MapFrom(src => src.DialogEndUserContext.SystemLabelId))
            .ForMember(dest => dest.FromPartyTransmissionsCount, opt => opt.MapFrom(src => (int)src.FromPartyTransmissionsCount))
            .ForMember(dest => dest.FromServiceOwnerTransmissionsCount, opt => opt.MapFrom(src => (int)src.FromServiceOwnerTransmissionsCount))
            .ForMember(dest => dest.Activities, opt => opt.MapFrom(src => src.Activities
                .OrderByDescending(activity => activity.CreatedAt)
                .ThenByDescending(activity => activity.Id)))
            .ForMember(dest => dest.SeenSinceLastUpdate, opt => opt.MapFrom(src => src.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.UpdatedAt)
                .OrderByDescending(x => x.CreatedAt)))
            .ForMember(dest => dest.SeenSinceLastContentUpdate, opt => opt.MapFrom(src => src.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.ContentUpdatedAt)
                .OrderByDescending(x => x.CreatedAt)))
            // Attachments and Content are mapped directly to avoid complex SQL generation.
            // Filtering and counting are handled in the second mapping stage.
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content));

        CreateMap<DialogEndUserContext, DialogEndUserContextDto>()
            .ForMember(dest => dest.SystemLabels, opt => opt.MapFrom(src => new List<SystemLabel.Values> { src.SystemLabelId }));

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
