using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // See IntermediateDialogContent
        CreateMap<IntermediateTransmissionContent, DialogTransmissionContent>();
        CreateMap<IntermediateDialogContent, DialogContent>();
        CreateMap<DialogTransmissionContent, ContentValueDto>();
        CreateMap<DialogContent, ContentValueDto>();
    }
}
