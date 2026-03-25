using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UpdateTransmissionDto, DialogTransmission>()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.TypeId, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.MapFrom(src => src.Sender.ToActor<DialogTransmissionSenderActor>()));

        CreateMap<TransmissionContentDto?, List<DialogTransmissionContent>?>()
            .ConvertUsing<TransmissionContentDtoToDialogTransmissionContentConverter<TransmissionContentDto>>();

        CreateMap<TransmissionAttachmentDto, DialogTransmissionAttachment>();

        CreateMap<TransmissionAttachmentUrlDto, AttachmentUrl>()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerType, opt => opt.Ignore())
            .ForMember(dest => dest.ConsumerTypeId, opt => opt.MapFrom(src => src.ConsumerType));

        CreateMap<TransmissionNavigationalActionDto, DialogTransmissionNavigationalAction>();
    }
}
