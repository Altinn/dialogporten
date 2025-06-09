using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

internal sealed class LocalizationSetConverter<TLocalizationSet> : ITypeConverter<IEnumerable<LocalizationDto>?, TLocalizationSet?>
    where TLocalizationSet : LocalizationSet, new()
{
    public TLocalizationSet? Convert(IEnumerable<LocalizationDto>? dtos, TLocalizationSet? set, ResolutionContext context)
    {
        if (!TryToCollection(dtos, out var concreteDtos) || concreteDtos.Count == 0)
        {
            return null;
        }

        set ??= new TLocalizationSet();
        set.Localizations.Merge(
            sources: concreteDtos,
            destinationKeySelector: x => x.LanguageCode,
            sourceKeySelector: x => x.LanguageCode,
            create: context.Mapper.Map<List<Localization>>,
            update: context.Mapper.Update,
            delete: DeleteDelegate.Default,
            comparer: StringComparer.InvariantCultureIgnoreCase);
        return set;
    }

    private static bool TryToCollection(
        IEnumerable<LocalizationDto>? dtos,
        [NotNullWhen(true)] out ICollection<LocalizationDto>? collection)
    {
        collection = dtos is not null
            ? dtos as ICollection<LocalizationDto> ?? dtos.ToList()
            : null;
        return collection is not null;
    }
}
