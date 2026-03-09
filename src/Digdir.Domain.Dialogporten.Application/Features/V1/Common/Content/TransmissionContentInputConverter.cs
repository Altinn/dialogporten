using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal sealed class IntermediateTransmissionContent
{
    public DialogTransmissionContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

internal static class TransmissionContentInputConverter
{
    public static List<DialogTransmissionContent>? ToEntities<TTransmissionContent>(
        TTransmissionContent? source,
        List<DialogTransmissionContent>? destinations = null)
        where TTransmissionContent : class, new()
    {
        if (source is null)
        {
            return null;
        }

        var sources = DialogTransmissionContentType.GetValues()
            .Select(transmissionContentType =>
            {
                if (!PropertyCache<TTransmissionContent>.PropertyByName.TryGetValue(transmissionContentType.Name, out var sourceProperty))
                {
                    return null;
                }

                if (sourceProperty.GetValue(source) is not ContentValueDto sourceValue)
                {
                    return null;
                }

                return new IntermediateTransmissionContent
                {
                    TypeId = transmissionContentType.Id,
                    Value = sourceValue.Value,
                    // Temporary converting of deprecated media types
                    // TODO: https://github.com/Altinn/dialogporten/issues/1782
                    MediaType = sourceValue.MediaType.MapDeprecatedMediaType()
                };
            })
            .OfType<IntermediateTransmissionContent>()
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

    public static TTransmissionContent? ToDto<TTransmissionContent>(List<DialogTransmissionContent>? sources)
        where TTransmissionContent : class, new()
    {
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        var destination = new TTransmissionContent();

        foreach (var source in sources)
        {
            if (!PropertyCache<TTransmissionContent>.PropertyByName.TryGetValue(source.TypeId.ToString(), out var property))
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
