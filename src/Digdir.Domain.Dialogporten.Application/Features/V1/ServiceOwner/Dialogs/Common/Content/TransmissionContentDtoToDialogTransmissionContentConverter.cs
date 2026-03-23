using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

// ReSharper disable ClassNeverInstantiated.Global

/// <summary>
/// This class is used to map between incoming dto objects and the internal transmission content structure.
/// Value needs to be mapped from a list of LocalizationDto in order for merging to work.
/// </summary>
internal sealed class IntermediateTransmissionContent
{
    public DialogTransmissionContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

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

        var sources = new List<IntermediateTransmissionContent>();

        foreach (var transmissionContentType in DialogTransmissionContentType.GetValues())
        {
            if (GetSourceValue(transmissionContentType.Id, source) is not { } sourceValue)
            {
                continue;
            }

            sources.Add(new IntermediateTransmissionContent
            {
                TypeId = transmissionContentType.Id,
                Value = sourceValue.Value,
                // Temporary converting of deprecated media types
                // TODO: https://github.com/Altinn/dialogporten/issues/1782
                MediaType = sourceValue.MediaType.MapDeprecatedMediaType()
            });
        }

        destinations ??= [];
        destinations
            .Merge(sources,
                destinationKeySelector: x => x.TypeId,
                sourceKeySelector: x => x.TypeId,
                create: context.Mapper.Map<List<DialogTransmissionContent>>,
                update: context.Mapper.Update,
                delete: DeleteDelegate.Default);

        return destinations;
    }

    private static ContentValueDto? GetSourceValue(DialogTransmissionContentType.Values transmissionContentType, TContentDto source) =>
        transmissionContentType switch
        {
            DialogTransmissionContentType.Values.Title => source.Title,
            DialogTransmissionContentType.Values.Summary => source.Summary,
            DialogTransmissionContentType.Values.ContentReference => source.ContentReference,
            _ => null
        };
}
