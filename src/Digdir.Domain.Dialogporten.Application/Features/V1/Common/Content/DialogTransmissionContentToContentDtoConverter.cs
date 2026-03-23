using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal sealed class DialogTransmissionContentToContentDtoConverter<TContentDto> :
    ITypeConverter<List<DialogTransmissionContent>?, TContentDto?>
    where TContentDto : class, ITransmissionContentDto, new()
{
    public TContentDto? Convert(List<DialogTransmissionContent>? sources, TContentDto? destination, ResolutionContext context)
    {
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        return sources.Aggregate(new TContentDto(), (dto, content) =>
        {
            switch (content.TypeId)
            {
                case DialogTransmissionContentType.Values.Title:
                    dto.Title = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogTransmissionContentType.Values.Summary:
                    dto.Summary = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogTransmissionContentType.Values.ContentReference:
                    dto.ContentReference = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                default:
                    throw new InvalidOperationException(
                        $"Unknown TypeId {content.TypeId} found in DialogTransmissionContent {content.Id}"
                    );
            }
        });
    }
}
