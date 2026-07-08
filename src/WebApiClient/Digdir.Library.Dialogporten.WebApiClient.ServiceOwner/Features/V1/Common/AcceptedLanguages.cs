using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public partial class AcceptedLanguages
{
    [JsonPropertyName("acceptedLanguage")]
    public ICollection<AcceptedLanguage>? AcceptedLanguage { get; set; }
}
