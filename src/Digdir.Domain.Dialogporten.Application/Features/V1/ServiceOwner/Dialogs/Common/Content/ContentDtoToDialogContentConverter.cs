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
        SyncContent(destinations, DialogContentType.Values.Title, source.Title);
        SyncContent(destinations, DialogContentType.Values.NonSensitiveTitle, source.NonSensitiveTitle);
        SyncContent(destinations, DialogContentType.Values.Summary, source.Summary);
        SyncContent(destinations, DialogContentType.Values.NonSensitiveSummary, source.NonSensitiveSummary);
        SyncContent(destinations, DialogContentType.Values.SenderName, source.SenderName);
        SyncContent(destinations, DialogContentType.Values.AdditionalInfo, source.AdditionalInfo);
        SyncContent(destinations, DialogContentType.Values.ExtendedStatus, source.ExtendedStatus);
        SyncContent(destinations, DialogContentType.Values.MainContentReference, source.MainContentReference);
        return destinations;
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
}
