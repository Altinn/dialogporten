using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Content;

public class AcceptedLanguages
{
    [JsonPropertyName("acceptedLanguage")]
    public ICollection<AcceptedLanguage>? AcceptedLanguage { get; set; }
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguages(
            AcceptedLanguage,
            static language => language.ToString());
}


public class AcceptedLanguage
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguage(LanguageCode, Weight);
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = null!;

    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}
