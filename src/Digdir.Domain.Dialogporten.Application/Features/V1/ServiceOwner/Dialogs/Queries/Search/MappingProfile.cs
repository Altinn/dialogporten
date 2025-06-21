using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
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
        CreateMap<IntermediateDialogDto, DialogDto>();
        CreateMap<DialogEntity, IntermediateDialogDto>()
            .ForMember(dest => dest.LatestActivity, opt => opt.MapFrom(src => src.Activities
                .OrderByDescending(activity => activity.CreatedAt).ThenByDescending(activity => activity.Id)
                .FirstOrDefault()
            ))
            .ForMember(dest => dest.SeenSinceLastUpdate, opt => opt.MapFrom(src => src.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.UpdatedAt)
                .OrderByDescending(x => x.CreatedAt)
            ))
            .ForMember(dest => dest.SeenSinceLastContentUpdate, opt => opt.MapFrom(src => src.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.ContentUpdatedAt)
                .OrderByDescending(x => x.CreatedAt)
            ))
            .ForMember(dest => dest.GuiAttachmentCount, opt => opt.MapFrom(src => src.Attachments
                .Count(x => x.Urls
                    .Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content.Where(x => x.Type.OutputInList)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

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
