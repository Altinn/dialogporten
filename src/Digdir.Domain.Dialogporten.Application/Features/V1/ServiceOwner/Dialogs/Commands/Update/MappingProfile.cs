using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ===========================================
        // ================== In =====================
        // ===========================================
        CreateMap<UpdateDialogDto, DialogEntity>()
            .IgnoreComplexDestinationProperties()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content));

        CreateMap<SearchTagDto, DialogSearchTag>()
            .IgnoreComplexDestinationProperties()
            .ForMember(x => x.Id, opt => opt.Ignore());

        CreateMap<ApiActionDto, DialogApiAction>()
            .IgnoreComplexDestinationProperties();

        CreateMap<ApiActionEndpointDto, DialogApiActionEndpoint>()
            .IgnoreComplexDestinationProperties()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(dest => dest.HttpMethod, opt => opt.Ignore())
            .ForMember(dest => dest.HttpMethodId, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<GuiActionDto, DialogGuiAction>()
            .IgnoreComplexDestinationProperties()
            .ForMember(dest => dest.Priority, opt => opt.Ignore())
            .ForMember(dest => dest.PriorityId, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.HttpMethod, opt => opt.Ignore())
            .ForMember(dest => dest.HttpMethodId, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<AttachmentDto, DialogAttachment>()
            .IgnoreComplexDestinationProperties();

        CreateMap<AttachmentUrlDto, AttachmentUrl>()
            .IgnoreComplexDestinationProperties()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));

        CreateMap<ContentDto?, List<DialogContent>?>()
            .ConvertUsing<DialogContentInputConverter<ContentDto>>();

        // Since these are append-only, we don't need to merge with existing
        // activity/transmission records and thus can map complex properties
        CreateMap<ActivityDto, DialogActivity>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type));

        CreateMap<TransmissionDto, DialogTransmission>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type));

        CreateMap<TransmissionContentDto?, List<DialogTransmissionContent>?>()
            .ConvertUsing<TransmissionContentInputConverter<TransmissionContentDto>>();

        CreateMap<TransmissionAttachmentDto, DialogTransmissionAttachment>();

        CreateMap<TransmissionAttachmentUrlDto, AttachmentUrl>()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));

        // ===========================================
        // ================== Patch ==================
        // ===========================================
        CreateMap<DialogDto, UpdateDialogDto>()
            // Remove all existing activities and transmissions, since these lists are append only and
            // existing activities/transmissions should not be considered in the update request.
            .ForMember(dest => dest.Activities, opt => opt.Ignore())
            .ForMember(dest => dest.Transmissions, opt => opt.Ignore());
        CreateMap<Queries.Get.SearchTagDto, SearchTagDto>();
        CreateMap<Queries.Get.DialogActivityDto, ActivityDto>();
        CreateMap<Queries.Get.DialogApiActionDto, ApiActionDto>();
        CreateMap<Queries.Get.DialogApiActionEndpointDto, ApiActionEndpointDto>();
        CreateMap<Queries.Get.DialogGuiActionDto, GuiActionDto>();
        CreateMap<Queries.Get.DialogAttachmentDto, AttachmentDto>();
        CreateMap<Queries.Get.DialogAttachmentUrlDto, AttachmentUrlDto>();
        CreateMap<Queries.Get.ContentDto, ContentDto>();
    }
}
