using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal static class ContentMapper
{
    extension(DialogContent source)
    {
        public ContentValueDto ToDto() => new()
        {
            Value = source.Value.ToDto() ?? [],
            MediaType = source.MediaType
        };
    }

    extension(DialogTransmissionContent source)
    {
        public ContentValueDto ToDto() => new()
        {
            Value = source.Value.ToDto() ?? [],
            MediaType = source.MediaType
        };
    }

    extension(IntermediateDialogContent source)
    {
        public DialogContent ToEntity() => new()
        {
            TypeId = source.TypeId,
            MediaType = source.MediaType,
            Value = ToRequiredLocalizationSet<DialogContentValue>(source.Value)
        };

        public void ApplyTo(DialogContent destination)
        {
            destination.TypeId = source.TypeId;
            destination.MediaType = source.MediaType;
            destination.Value = source.Value.MergeInto(destination.Value) ?? new DialogContentValue();
        }
    }

    extension(IntermediateTransmissionContent source)
    {
        public DialogTransmissionContent ToEntity() => new()
        {
            TypeId = source.TypeId,
            MediaType = source.MediaType,
            Value = ToRequiredLocalizationSet<DialogTransmissionContentValue>(source.Value)
        };

        public void ApplyTo(DialogTransmissionContent destination)
        {
            destination.TypeId = source.TypeId;
            destination.MediaType = source.MediaType;
            destination.Value = source.Value.MergeInto(destination.Value) ?? new DialogTransmissionContentValue();
        }
    }

    public static List<DialogContent>? ToDialogContentEntities<TDialogContent>(
        this TDialogContent? source,
        List<DialogContent>? destinations = null)
        where TDialogContent : class, new()
        => DialogContentInputConverter.ToEntities(source, destinations);

    public static TDialogContent? ToDialogContentDto<TDialogContent>(this List<DialogContent>? source)
        where TDialogContent : class, new()
        => DialogContentInputConverter.ToDto<TDialogContent>(source);

    public static List<DialogTransmissionContent>? ToDialogTransmissionContentEntities<TTransmissionContent>(
        this TTransmissionContent? source,
        List<DialogTransmissionContent>? destinations = null)
        where TTransmissionContent : class, new()
        => TransmissionContentInputConverter.ToEntities(source, destinations);

    public static TTransmissionContent? ToDialogTransmissionContentDto<TTransmissionContent>(
        this List<DialogTransmissionContent>? source)
        where TTransmissionContent : class, new()
        => TransmissionContentInputConverter.ToDto<TTransmissionContent>(source);

    private static TLocalizationSet ToRequiredLocalizationSet<TLocalizationSet>(IEnumerable<LocalizationDto> source)
        where TLocalizationSet : LocalizationSet, new()
        => source.MergeInto<TLocalizationSet>(null) ?? new TLocalizationSet();
}
