using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogDto, Dialog>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.EnduserContextRevision, opt => opt.MapFrom(src => src.EnduserContextRevision));

        CreateMap<DialogAttachmentDto, Attachment>();
        CreateMap<DialogAttachmentUrlDto, AttachmentUrl>()
            .ForMember(dest => dest.ConsumerType, opt => opt.MapFrom(src => src.ConsumerType));

        CreateMap<DialogGuiActionDto, GuiAction>()
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.HttpMethod, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<DialogApiActionDto, ApiAction>();
        CreateMap<DialogApiActionEndpointDto, ApiActionEndpoint>()
            .ForMember(dest => dest.HttpMethod, opt => opt.MapFrom(src => src.HttpMethod));

        CreateMap<ContentDto, Content>();
        CreateMap<DialogSeenLogDto, SeenLog>();

        CreateMap<DialogTransmissionDto, Transmission>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        CreateMap<DialogTransmissionAttachmentDto, Attachment>();
        CreateMap<DialogTransmissionAttachmentUrlDto, AttachmentUrl>()
            .ForMember(dest => dest.ConsumerType, opt => opt.MapFrom(src => src.ConsumerType));
        CreateMap<DialogTransmissionContentDto, TransmissionContent>();
    }
}
