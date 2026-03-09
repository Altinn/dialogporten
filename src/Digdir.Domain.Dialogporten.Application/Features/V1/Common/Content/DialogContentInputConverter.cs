using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal sealed class IntermediateDialogContent
{
    public DialogContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

internal static class DialogContentInputConverter
{
    public static List<DialogContent>? ToEntities<TDialogContent>(
        TDialogContent? source,
        List<DialogContent>? destinations = null)
        where TDialogContent : class, new()
    {
        if (source is null)
        {
            return null;
        }

        var sources = DialogContentType.GetValues()
            .Select(dialogContentType =>
            {
                if (!PropertyCache<TDialogContent>.PropertyByName.TryGetValue(dialogContentType.Name, out var sourceProperty))
                {
                    return null;
                }

                if (sourceProperty.GetValue(source) is not ContentValueDto sourceValue)
                {
                    return null;
                }

                return new IntermediateDialogContent
                {
                    TypeId = dialogContentType.Id,
                    Value = sourceValue.Value,
                    // Temporary converting of deprecated media types
                    // TODO: https://github.com/Altinn/dialogporten/issues/1782
                    MediaType = sourceValue.MediaType.MapDeprecatedMediaType()
                };
            })
            .OfType<IntermediateDialogContent>()
            .ToList();

        destinations ??= [];
        destinations
            .Merge(sources,
                destinationKeySelector: x => x.TypeId,
                sourceKeySelector: x => x.TypeId,
                create: createSet => createSet.Select(content => content.ToEntity()).ToList(),
                update: updateSets =>
                {
                    foreach (var (updateSource, updateDestination) in updateSets)
                    {
                        updateSource.ApplyTo(updateDestination);
                    }
                },
                delete: DeleteDelegate.Default);

        return destinations;
    }

    public static TDialogContent? ToDto<TDialogContent>(List<DialogContent>? sources)
        where TDialogContent : class, new()
    {
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        var destination = new TDialogContent();

        foreach (var source in sources)
        {
            if (!PropertyCache<TDialogContent>.PropertyByName.TryGetValue(source.TypeId.ToString(), out var property))
            {
                continue;
            }

            property.SetValue(destination, source.ToDto());
        }

        return destination;
    }
}

file sealed class PropertyCache<T>
{
    public static readonly Dictionary<string, PropertyInfo> PropertyByName = typeof(T)
        .GetProperties()
        .ToDictionary(x => x.Name, StringComparer.InvariantCultureIgnoreCase);
}
