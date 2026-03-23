using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

// ReSharper disable ClassNeverInstantiated.Global

internal sealed class ContentDtoToDialogContentConverter<TContentDto> :
    ITypeConverter<TContentDto?, List<DialogContent>?>
    where TContentDto : class, IDialogContentDto
{
    public List<DialogContent>? Convert(TContentDto? source, List<DialogContent>? destinations, ResolutionContext context)
    {
        if (source is null)
        {
            return null;
        }

        destinations ??= [];
        foreach (var contentType in DialogContentType.GetValues())
        {
            var sourceValue = GetSourceValue(contentType.Id, source);
            SyncContent(destinations, contentType.Id, sourceValue);
        }

        return destinations;
    }

    private static ContentValueDto? GetSourceValue(DialogContentType.Values typeId, TContentDto source) =>
        typeId switch
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
                $"Unknown {nameof(DialogContentType)} '{typeId}'")
        };

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

        // Temporary converting of deprecated media types
        // TODO: https://github.com/Altinn/dialogporten/issues/1782
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
