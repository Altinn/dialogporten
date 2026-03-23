using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

// ReSharper disable ClassNeverInstantiated.Global

/// <summary>
/// This class is used to map between incoming dto objects and the internal dialog content structure.
/// Value needs to be mapped from a list of LocalizationDto in order for merging to work.
/// </summary>
internal sealed class IntermediateDialogContent
{
    public DialogContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

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

        var sources = new List<IntermediateDialogContent>();

        foreach (var dialogContentType in DialogContentType.GetValues())
        {
            if (GetSourceValue(dialogContentType.Id, source) is not { } sourceValue)
            {
                continue;
            }

            sources.Add(new IntermediateDialogContent
            {
                TypeId = dialogContentType.Id,
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
                create: ContentMapper.CreateDialogContents,
                update: ContentMapper.UpdateDialogContents,
                delete: DeleteDelegate.Default);

        return destinations;
    }

    private static ContentValueDto? GetSourceValue(DialogContentType.Values dialogContentType, TContentDto source) =>
        dialogContentType switch
        {
            DialogContentType.Values.Title => source.Title,
            DialogContentType.Values.NonSensitiveTitle => source.NonSensitiveTitle,
            DialogContentType.Values.Summary => source.Summary,
            DialogContentType.Values.NonSensitiveSummary => source.NonSensitiveSummary,
            DialogContentType.Values.SenderName => source.SenderName,
            DialogContentType.Values.AdditionalInfo => source.AdditionalInfo,
            DialogContentType.Values.ExtendedStatus => source.ExtendedStatus,
            DialogContentType.Values.MainContentReference => source.MainContentReference,
            _ => null
        };
}
