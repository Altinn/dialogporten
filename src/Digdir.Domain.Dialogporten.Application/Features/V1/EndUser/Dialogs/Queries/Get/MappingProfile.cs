using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogEntity, DialogDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
            .ForMember(dest => dest.SystemLabel, opt => opt.MapFrom(src =>
                src.EndUserContext.DialogEndUserContextSystemLabels
                    .First(l => SystemLabel.MutuallyExclusiveRequiredLabels.Contains(l.SystemLabelId))
                    .SystemLabelId))
            .ForMember(dest => dest.FromPartyTransmissionsCount, opt => opt
                .MapFrom(src => (int)src.FromPartyTransmissionsCount))
            .ForMember(dest => dest.FromServiceOwnerTransmissionsCount, opt => opt
                .MapFrom(src => (int)src.FromServiceOwnerTransmissionsCount))
            .ForMember(dest => dest.SeenSinceLastUpdate, opt => opt.Ignore());

        CreateMap<DialogEndUserContext, DialogEndUserContextDto>()
            .ForMember(dest => dest.SystemLabels, opt => opt
                .MapFrom(src => src.DialogEndUserContextSystemLabels
                    .Select(x => x.SystemLabelId)
                    .ToList()));

        CreateMap<DialogSeenLog, DialogSeenLogDto>()
            .ForMember(dest => dest.SeenAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<DialogActivity, DialogActivityDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId));

        CreateMap<DialogApiAction, DialogApiActionDto>();

        CreateMap<DialogApiActionEndpoint, DialogApiActionEndpointDto>()
            .ForMember(dest => dest.HttpMethod, opt => opt.MapFrom(src => src.HttpMethodId));

        CreateMap<DialogGuiAction, DialogGuiActionDto>()
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.PriorityId))
            .ForMember(dest => dest.HttpMethod, opt => opt.MapFrom(src => src.HttpMethodId));

        CreateMap<DialogAttachment, DialogAttachmentDto>();

        CreateMap<AttachmentUrl, DialogAttachmentUrlDto>()
            .ForMember(dest => dest.ConsumerType, opt => opt.MapFrom(src => src.ConsumerTypeId));

        CreateMap<List<DialogContent>?, ContentDto?>()
            .ConvertUsing<DialogContentOutputConverter<ContentDto>>();

        CreateMap<List<DialogTransmissionContent>?, DialogTransmissionContentDto?>()
            .ConvertUsing<TransmissionContentOutputConverter<DialogTransmissionContentDto>>();

        CreateMap<DialogTransmission, DialogTransmissionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId))
            .ForMember(dest => dest.IsOpened, opt => opt.MapFrom(src => DialogUnopenedContent.IsOpened(src)));

        CreateMap<DialogTransmissionAttachment, DialogTransmissionAttachmentDto>();
        CreateMap<AttachmentUrl, DialogTransmissionAttachmentUrlDto>()
            .ForMember(dest => dest.ConsumerType, opt => opt.MapFrom(src => src.ConsumerTypeId));
    }

}
