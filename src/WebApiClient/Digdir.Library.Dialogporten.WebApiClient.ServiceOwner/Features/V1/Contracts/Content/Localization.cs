using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Content;

public class Localization
{
    /// <summary>
    /// The localized text (or URL if a front-channel embed).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// The language code of the localization in ISO 639-1 format.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = null!;
}