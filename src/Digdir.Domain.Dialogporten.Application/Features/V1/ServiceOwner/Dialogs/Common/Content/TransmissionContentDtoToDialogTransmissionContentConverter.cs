using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

// ReSharper disable ClassNeverInstantiated.Global

internal sealed class TransmissionContentDtoToDialogTransmissionContentConverter<TContentDto> :
    ITypeConverter<TContentDto?, List<DialogTransmissionContent>?>
    where TContentDto : class, ITransmissionContentDto
{
    public List<DialogTransmissionContent>? Convert(TContentDto? source, List<DialogTransmissionContent>? destinations, ResolutionContext context)
    {
        if (source is null)
        {
            return null;
        }

        destinations ??= [];
        SyncContent(destinations, DialogTransmissionContentType.Values.Title, source.Title);
        SyncContent(destinations, DialogTransmissionContentType.Values.Summary, source.Summary);
        SyncContent(destinations, DialogTransmissionContentType.Values.ContentReference, source.ContentReference);
        return destinations;
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

        // Temporary converting of deprecated media types
        // TODO: https://github.com/Altinn/dialogporten/issues/1782
        var mediaType = sourceValue.MediaType.MapDeprecatedMediaType();

        if (existing is not null)
        {
            existing.MediaType = mediaType;
            existing.Value.Localizations.MergeFrom(sourceValue.Value);
        }
        else
        {
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
}
