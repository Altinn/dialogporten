using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public partial class AcceptedLanguage
{
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = default!;

    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}
