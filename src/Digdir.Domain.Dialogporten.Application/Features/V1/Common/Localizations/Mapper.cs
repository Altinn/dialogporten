using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

internal static class LocalizationMapper
{
    extension(LocalizationSet? source)
    {
        public List<LocalizationDto>? ToDto() => source?.Localizations
            .Select(localization => localization.ToDto())
            .ToList();
    }

    extension(Localization source)
    {
        public LocalizationDto ToDto() => new()
        {
            LanguageCode = source.LanguageCode,
            Value = source.Value
        };
    }

    extension(LocalizationDto source)
    {
        public Localization ToEntity() => new()
        {
            LanguageCode = source.LanguageCode,
            Value = source.Value
        };
    }

    extension(IEnumerable<LocalizationDto>? source)
    {
        public TLocalizationSet? MergeInto<TLocalizationSet>(TLocalizationSet? destination)
            where TLocalizationSet : LocalizationSet, new()
        {
            var concreteSource = source as ICollection<LocalizationDto> ?? source?.ToList();
            if (concreteSource is null || concreteSource.Count == 0)
            {
                return null;
            }

            destination ??= new TLocalizationSet();
            destination.Localizations.Merge(
                sources: concreteSource,
                destinationKeySelector: localization => localization.LanguageCode,
                sourceKeySelector: localization => localization.LanguageCode,
                create: createSet => createSet.Select(localization => localization.ToEntity()).ToList(),
                update: updateSets =>
                {
                    foreach (var (updateSource, updateDestination) in updateSets)
                    {
                        updateDestination.LanguageCode = updateSource.LanguageCode;
                        updateDestination.Value = updateSource.Value;
                    }
                },
                delete: DeleteDelegate.Default,
                comparer: StringComparer.InvariantCultureIgnoreCase);

            return destination;
        }
    }
}
