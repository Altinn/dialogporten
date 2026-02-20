using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Content;

public sealed class DialogContentToContentDtoConverter : ITypeConverter<List<DialogContent>?, ContentDto?>
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
                case DialogContentType.Values.NonSensitiveTitle:
                    dto.Title = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogContentType.Values.SenderName:
                    dto.SenderName = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogContentType.Values.Summary:
                case DialogContentType.Values.NonSensitiveSummary:
                    dto.Summary = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogContentType.Values.AdditionalInfo:
                    dto.AdditionalInfo = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogContentType.Values.ExtendedStatus:
                    dto.ExtendedStatus = context.Mapper.Map<ContentValueDto>(content);
                    return dto;
                case DialogContentType.Values.MainContentReference:
                    dto.MainContentReference = context.Mapper.Map<AuthorizationContentValueDto>(content);
                    return dto;
                default:
                    throw new InvalidOperationException(
                        $"Unknown TypeId {content.TypeId} found in DialogContent {content.Id}"
                    );
            }
        });
    }
}
