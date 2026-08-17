using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UpdateTransmissionDto, DialogTransmission>()
            .ForMember(dest => dest.CreatedAt, opt =>
            {
                opt.PreCondition(src => src.CreatedAt.HasValue);
                opt.MapFrom(src => src.CreatedAt!.Value);
            })
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            // Handled manually in the command handler (in-place replace with orphan-delete semantics)
            .ForMember(dest => dest.AuthorizationContext, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.MapFrom(src => src.Sender.ToActor<DialogTransmissionSenderActor>()));

        CreateMap<TransmissionContentDto?, List<DialogTransmissionContent>?>()
            .ConvertUsing<TransmissionContentDtoToDialogTransmissionContentConverter<TransmissionContentDto>>();

        CreateMap<TransmissionAttachmentDto, DialogTransmissionAttachment>()
            // Handled manually in the command handler (in-place replace with orphan-delete semantics)
            .ForMember(dest => dest.AuthorizationContext, opt => opt.Ignore());

        CreateMap<TransmissionAttachmentUrlDto, AttachmentUrl>()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));

        CreateMap<TransmissionNavigationalActionDto, DialogTransmissionNavigationalAction>();

        CreateMap<ChildAuthorizationContextDto, DialogTransmissionNavigationalActionAuthorizationContext>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            // The XACML action for navigational actions is always "read" and cannot be overridden
            .ForMember(dest => dest.Action, opt => opt.Ignore())
            .ForMember(dest => dest.UnauthorizedPresentation, opt => opt.Ignore())
            .ForMember(dest => dest.UnauthorizedPresentation, opt => opt.MapFrom(src => src.UnauthorizedPresentation))
            .ForMember(dest => dest.NavigationalActionId, opt => opt.Ignore())
            .ForMember(dest => dest.NavigationalAction, opt => opt.Ignore());
    }
}
