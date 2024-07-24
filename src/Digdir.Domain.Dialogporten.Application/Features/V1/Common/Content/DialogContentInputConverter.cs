using System.Reflection;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Content;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

/// <summary>
/// TODO: Discuss this with the team later. It works for now
/// This class is used to map bewteen the incoming dto object and the internal dialog content structure.
/// Value needs to be mapped from a list of LocalizationDto in order for merging to work.
/// </summary>
internal class IntermediateDialogContent
{
    public DialogContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

internal class DialogContentInputConverter<TDialogContent> :
    ITypeConverter<TDialogContent?, List<DialogContent>?>
    where TDialogContent : class, new()
{
    public List<DialogContent>? Convert(TDialogContent? source, List<DialogContent>? destinations, ResolutionContext context)
    {
        if (source is null)
        {
            return null;
        }

        var sources = new List<IntermediateDialogContent>();

        foreach (var dialogContentType in DialogContentType.GetValues())
        {
            if (!PropertyCache<TDialogContent>.PropertyByName.TryGetValue(dialogContentType.Name, out var sourceProperty))
            {
                continue;
            }

            if (sourceProperty.GetValue(source) is not DialogContentValueDto sourceValue)
            {
                continue;
            }

            sources.Add(new IntermediateDialogContent
            {
                TypeId = dialogContentType.Id,
                Value = sourceValue.Value,
                MediaType = sourceValue.MediaType
            });
        }

        destinations ??= [];
        destinations
            .Merge(sources,
                destinationKeySelector: x => x.TypeId,
                sourceKeySelector: x => x.TypeId,
                create: context.Mapper.Map<List<DialogContent>>,
                update: context.Mapper.Update,
                delete: DeleteDelegate.NoOp);

        return destinations;
    }
}

internal class DialogContentOutputConverter<TDialogContent> :
    ITypeConverter<List<DialogContent>?, TDialogContent?>
    where TDialogContent : class, new()
{
    public TDialogContent? Convert(List<DialogContent>? sources, TDialogContent? destination, ResolutionContext context)
    {
        if (sources is null)
        {
            return null;
        }

        destination ??= new TDialogContent();

        foreach (var source in sources)
        {
            if (!PropertyCache<TDialogContent>.PropertyByName.TryGetValue(source.TypeId.ToString(), out var property))
            {
                continue;
            }

            property.SetValue(destination, context.Mapper.Map<DialogContentValueDto>(source));
        }

        return destination;
    }
}

file class PropertyCache<T>
{
    public static readonly Dictionary<string, PropertyInfo> PropertyByName = typeof(T)
        .GetProperties()
        .ToDictionary(x => x.Name, StringComparer.InvariantCultureIgnoreCase);
}
