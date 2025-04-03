using AutoMapper;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

// ReSharper disable ClassNeverInstantiated.Global

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

/// <summary>
/// TODO: Discuss this with the team later. It works for now
/// This class is used to map between the incoming dto object and the internal transmission content structure.
/// Value needs to be mapped from a list of LocalizationDto in order for merging to work.
///
/// We might want to consider combining this class with DialogContentInputConverter later.
/// </summary>

internal sealed class IntermediateTransmissionContent
{
    public DialogTransmissionContentType.Values TypeId { get; set; }
    public List<LocalizationDto> Value { get; set; } = null!;
    public string MediaType { get; set; } = MediaTypes.PlainText;
}

internal sealed class TransmissionContentInputConverter<TTransmissionContent> :
    ITypeConverter<TTransmissionContent?, List<DialogTransmissionContent>?>
    where TTransmissionContent : class, new()
{
    public List<DialogTransmissionContent>? Convert(TTransmissionContent? source, List<DialogTransmissionContent>? destinations, ResolutionContext context)
    {
        if (source is null)
        {
            return null;
        }

        var sources = new List<IntermediateTransmissionContent>();

        foreach (var transmissionContentType in DialogTransmissionContentType.GetValues())
        {
            if (!PropertyCache<TTransmissionContent>.PropertyByName.TryGetValue(transmissionContentType.Name, out var sourceProperty))
            {
                continue;
            }

            if (sourceProperty.GetValue(source) is not ContentValueDto sourceValue)
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
                delete: DeleteDelegate.NoOp);

        return destinations;
    }
}

internal sealed class TransmissionContentOutputConverter<TTransmissionContent> :
    ITypeConverter<List<DialogTransmissionContent>?, TTransmissionContent?>
    where TTransmissionContent : class, new()
{
    public TTransmissionContent? Convert(List<DialogTransmissionContent>? sources, TTransmissionContent? destination, ResolutionContext context)
    {
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        destination ??= new TTransmissionContent();

        foreach (var source in sources)
        {
            if (!PropertyCache<TTransmissionContent>.PropertyByName.TryGetValue(source.TypeId.ToString(), out var property))
            {
                continue;
            }

            property.SetValue(destination, context.Mapper.Map<ContentValueDto>(source));
        }

        return destination;
    }
}

// ReSharper disable once ClassNeverInstantiated.Local
file sealed class PropertyCache<T>
{
    public static readonly Dictionary<string, PropertyInfo> PropertyByName = typeof(T)
        .GetProperties()
        .ToDictionary(x => x.Name, StringComparer.InvariantCultureIgnoreCase);
}
