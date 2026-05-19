using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;
using Digdir.Domain.Dialogporten.Domain;
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

internal static class TransmissionContentListMapExtensions
{
    extension(List<DialogTransmissionContent>? sources)
    {
        internal TContentDto? ToTransmissionContentDto<TContentDto>()
            where TContentDto : class, ITransmissionContentDto, new() =>
            sources is null || sources.Count == 0
                ? null
                : sources.Aggregate(new TContentDto(), (dto, content) =>
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

internal static class DialogContentDtoMapExtensions
{
    extension<TContentDto>(TContentDto? source)
        where TContentDto : class, IDialogContentDto
    {
        internal List<DialogContent>? ToDialogContentList(List<DialogContent>? destinations)
        {
            if (source is null)
            {
                return null;
            }

            destinations ??= [];
            foreach (var contentType in DialogContentType.GetValues())
            {
                var sourceValue = contentType.Id switch
                {
                    DialogContentType.Values.Title => source.Title,
                    DialogContentType.Values.NonSensitiveTitle => source.NonSensitiveTitle,
                    DialogContentType.Values.Summary => source.Summary,
                    DialogContentType.Values.NonSensitiveSummary => source.NonSensitiveSummary,
                    DialogContentType.Values.SenderName => source.SenderName,
                    DialogContentType.Values.AdditionalInfo => source.AdditionalInfo,
                    DialogContentType.Values.ExtendedStatus => source.ExtendedStatus,
                    DialogContentType.Values.MainContentReference => source.MainContentReference,
                    _ => throw new InvalidOperationException(
                        $"Unknown {nameof(DialogContentType)} '{contentType.Id}'")
                };

                SyncContent(destinations, contentType.Id, sourceValue);
            }

            return destinations;
        }
    }

    private static void SyncContent(
        List<DialogContent> destinations,
        DialogContentType.Values typeId,
        ContentValueDto? sourceValue)
    {
        var existing = destinations.FirstOrDefault(x => x.TypeId == typeId);

        if (sourceValue is null)
        {
            if (existing is not null)
            {
                destinations.Remove(existing);
            }

            return;
        }

        var mediaType = sourceValue.MediaType.MapDeprecatedMediaType();

        if (existing is not null)
        {
            existing.MediaType = mediaType;
            existing.Value.Localizations.MergeFrom(sourceValue.Value);
            return;
        }

        destinations.Add(new DialogContent
        {
            TypeId = typeId,
            MediaType = mediaType,
            Value = new DialogContentValue
            {
                Localizations = sourceValue.Value.Select(x => x.ToLocalization()).ToList()
            }
        });
    }
}

internal static class TransmissionContentDtoMapExtensions
{
    extension<TContentDto>(TContentDto? source)
        where TContentDto : class, ITransmissionContentDto
    {
        internal List<DialogTransmissionContent>? ToDialogTransmissionContentList(
            List<DialogTransmissionContent>? destinations)
        {
            if (source is null)
            {
                return null;
            }

            destinations ??= [];
            foreach (var contentType in DialogTransmissionContentType.GetValues())
            {
                var sourceValue = contentType.Id switch
                {
                    DialogTransmissionContentType.Values.Title => source.Title,
                    DialogTransmissionContentType.Values.Summary => source.Summary,
                    DialogTransmissionContentType.Values.ContentReference => source.ContentReference,
                    _ => throw new InvalidOperationException(
                        $"Unknown {nameof(DialogTransmissionContentType)} '{contentType.Id}'")
                };

                SyncContent(destinations, contentType.Id, sourceValue);
            }

            return destinations;
        }
    }

    private static void SyncContent(
        List<DialogTransmissionContent> destinations,
        DialogTransmissionContentType.Values typeId,
        ContentValueDto? sourceValue)
    {
        var existing = destinations.FirstOrDefault(x => x.TypeId == typeId);

        if (sourceValue is null)
        {
            if (existing is not null)
            {
                destinations.Remove(existing);
            }

            return;
        }

        var mediaType = sourceValue.MediaType.MapDeprecatedMediaType();

        if (existing is not null)
        {
            existing.MediaType = mediaType;
            existing.Value.Localizations.MergeFrom(sourceValue.Value);
            return;
        }

        destinations.Add(new DialogTransmissionContent
        {
            TypeId = typeId,
            MediaType = mediaType,
            Value = new DialogTransmissionContentValue
            {
                Localizations = sourceValue.Value.Select(x => x.ToLocalization()).ToList()
            }
        });
    }
}
