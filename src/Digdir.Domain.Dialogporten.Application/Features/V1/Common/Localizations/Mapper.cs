using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

internal static class LocalizationDtoMapExtensions
{
    extension(LocalizationDto dto)
    {
        internal Localization ToLocalization() =>
            new()
            {
                Value = dto.Value,
                LanguageCode = dto.LanguageCode
            };
    }
}

internal static class LocalizationMapExtensions
{
    extension(Localization localization)
    {
        internal LocalizationDto ToDto() =>
            new()
            {
                Value = localization.Value,
                LanguageCode = localization.LanguageCode
            };
    }
}

internal static class LocalizationSetMapExtensions
{
    extension(LocalizationSet? set)
    {
        internal List<LocalizationDto>? ToDtoList() =>
            set?.Localizations.Select(x => x.ToDto()).ToList();
    }

    extension(ICollection<Localization> localizations)
    {
        internal void MergeFrom(ICollection<LocalizationDto> dtos) =>
            localizations.Merge(
                sources: dtos,
                destinationKeySelector: x => x.LanguageCode,
                sourceKeySelector: x => x.LanguageCode,
                create: creatables => creatables.Select(x => x.ToLocalization()).ToList(),
                update: UpdateLocalizations,
                delete: DeleteDelegate.Default,
                comparer: StringComparer.InvariantCultureIgnoreCase);
    }

    private static void UpdateLocalizations(
        IEnumerable<UpdateSet<Localization, LocalizationDto>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            destination.Value = source.Value;
            destination.LanguageCode = source.LanguageCode;
        }
    }
}
