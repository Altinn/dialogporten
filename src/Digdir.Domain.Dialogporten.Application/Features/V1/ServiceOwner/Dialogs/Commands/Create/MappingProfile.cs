using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateDialogDto, DialogEntity>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status));
        CreateMap<SearchTagDto, DialogSearchTag>();

        CreateMap<ServiceOwnerContext, Domain.ServiceOwnerContexts.Entities.ServiceOwnerContext>();
        CreateMap<ServiceOwnerLabelDto, ServiceOwnerLabel>();

        CreateMap<AttachmentDto, DialogAttachment>();
        CreateMap<AttachmentUrlDto, AttachmentUrl>()
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));

        CreateMap<GuiActionDto, DialogGuiAction>()
            .ForMember(dest => dest.Priority, opt => opt.Ignore())
            .ForMember(dest => dest.PriorityId, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.HttpMethod, opt => opt.Ignore())
            .ForMember(dest => dest.HttpMethodId, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<ApiActionDto, DialogApiAction>();

        CreateMap<ApiActionEndpointDto, DialogApiActionEndpoint>()
            .ForMember(dest => dest.HttpMethod, opt => opt.Ignore())
            .ForMember(dest => dest.HttpMethodId, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<ActivityDto, DialogActivity>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type));

        CreateMap<ContentDto?, List<DialogContent>?>()
            .ConvertUsing<DialogContentInputConverter<ContentDto>>();

        CreateMap<TransmissionContentDto?, List<DialogTransmissionContent>?>()
            .ConvertUsing<TransmissionContentInputConverter<TransmissionContentDto>>();

        CreateMap<TransmissionDto, DialogTransmission>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type));

        CreateMap<TransmissionAttachmentDto, DialogTransmissionAttachment>();
        CreateMap<TransmissionAttachmentUrlDto, AttachmentUrl>()
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));
    }
}
