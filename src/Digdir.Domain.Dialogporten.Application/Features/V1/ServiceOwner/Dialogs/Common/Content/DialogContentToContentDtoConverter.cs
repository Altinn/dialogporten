using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

internal sealed class DialogContentToContentDtoConverter : ITypeConverter<List<DialogContent>?, ContentDto?>
{
    public ContentDto? Convert(List<DialogContent>? sources, ContentDto? destination, ResolutionContext context)
    {
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        return sources.Aggregate(new ContentDto(), (dto, content) =>
        {
            switch (content.TypeId)
            {
                case DialogContentType.Values.Title:
                    dto.Title = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.NonSensitiveTitle:
                    dto.NonSensitiveTitle = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.SenderName:
                    dto.SenderName = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.Summary:
                    dto.Summary = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.NonSensitiveSummary:
                    dto.NonSensitiveSummary = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.AdditionalInfo:
                    dto.AdditionalInfo = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.ExtendedStatus:
                    dto.ExtendedStatus = content.ToContentValueDto();
                    return dto;
                case DialogContentType.Values.MainContentReference:
                    dto.MainContentReference = content.ToContentValueDto();
                    return dto;
                default:
                    throw new InvalidOperationException(
                        $"Unknown TypeId {content.TypeId} found in DialogContent {content.Id}"
                    );
            }
        });
    }
}
