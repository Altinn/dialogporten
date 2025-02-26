using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

public sealed class LocalizationDto
{
    private readonly string _languageCode = null!;

    /// <summary>
    /// The localized text (or URL if a front-channel embed).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The language code of the localization in ISO 639-1 format.
    /// </summary>
    /// <example>nb</example>
    public required string LanguageCode
    {
        get => _languageCode;
        init => _languageCode = Localization.NormalizeCultureCode(value)!;
    }
}
