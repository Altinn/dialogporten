using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal static class DialogTransmissionContentMapExtensions
{
    extension(DialogTransmissionContent content)
    {
        internal ContentValueDto ToContentValueDto() =>
            new()
            {
                Value = content.Value.ToDtoList() ?? [],
                MediaType = content.MediaType
            };
    }
}

internal static class DialogContentMapExtensions
{
    extension(DialogContent content)
    {
        internal ContentValueDto ToContentValueDto() =>
            new()
            {
                Value = content.Value.ToDtoList() ?? [],
                MediaType = content.MediaType
            };
    }
}

internal static class ContentMapper
{
    internal static TContentDto? ToTransmissionContentDto<TContentDto>(
        this List<DialogTransmissionContent>? sources)
        where TContentDto : class, ITransmissionContentDto, new()
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
                    dto.Title = content.ToContentValueDto();
                    return dto;
                case DialogTransmissionContentType.Values.Summary:
                    dto.Summary = content.ToContentValueDto();
                    return dto;
                case DialogTransmissionContentType.Values.ContentReference:
                    dto.ContentReference = content.ToContentValueDto();
                    return dto;
                default:
                    throw new InvalidOperationException(
                        $"Unknown TypeId {content.TypeId} found in DialogTransmissionContent {content.Id}");
            }
        });
    }
}
