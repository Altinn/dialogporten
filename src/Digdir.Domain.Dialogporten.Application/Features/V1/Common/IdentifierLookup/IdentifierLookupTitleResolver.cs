using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal static class IdentifierLookupTitleResolver
{
    public static List<LocalizationDto> ResolveEndUserTitle(
        IdentifierLookupDialogData dialogData,
        int currentAuthenticationLevel,
        int minimumAuthenticationLevel) =>
        ToLocalizations(
            currentAuthenticationLevel < minimumAuthenticationLevel && dialogData.NonSensitiveTitle is not null
                ? dialogData.NonSensitiveTitle
                : dialogData.Title);

    public static List<LocalizationDto> ToLocalizations(IEnumerable<ResourceLocalization> localizations) =>
        localizations
            .Select(x => new LocalizationDto
            {
                LanguageCode = x.LanguageCode,
                Value = x.Value
            })
            .ToList();
}
