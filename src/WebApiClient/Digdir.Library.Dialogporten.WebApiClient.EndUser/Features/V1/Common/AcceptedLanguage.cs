using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

public class AcceptedLanguage
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguage(LanguageCode, Weight);

    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = null!;

    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}
