using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogEntity, DialogDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
            .ForMember(dest => dest.EnduserContextRevision, opt => opt.MapFrom(src => src.DialogEndUserContext.Revision))
            .ForMember(dest => dest.SericeOwnerContextRevision, opt => opt.MapFrom(src => src.ServiceOwnerContext.Revision))
            .ForMember(dest => dest.SeenSinceLastUpdate, opt => opt.Ignore())
            .ForMember(dest => dest.SystemLabel, opt => opt.MapFrom(src => src.DialogEndUserContext.SystemLabelId));

        CreateMap<DialogServiceOwnerContext, DialogServiceOwnerContextDto>();
        CreateMap<DialogServiceOwnerLabel, DialogServiceOwnerLabelDto>();

        CreateMap<DialogSeenLogSeenByActor, ActorDto>();
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

        CreateMap<DialogSearchTag, SearchTagDto>();

        CreateMap<List<DialogContent>?, ContentDto?>()
            .ConvertUsing<DialogContentOutputConverter<ContentDto>>();

        CreateMap<List<DialogTransmissionContent>?, DialogTransmissionContentDto?>()
            .ConvertUsing<TransmissionContentOutputConverter<DialogTransmissionContentDto>>();

        CreateMap<DialogTransmission, DialogTransmissionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TypeId));

        CreateMap<DialogTransmissionAttachment, DialogTransmissionAttachmentDto>();
        CreateMap<AttachmentUrl, DialogTransmissionAttachmentUrlDto>()
            .ForMember(dest => dest.ConsumerType, opt => opt.MapFrom(src => src.ConsumerTypeId));
    }
}
