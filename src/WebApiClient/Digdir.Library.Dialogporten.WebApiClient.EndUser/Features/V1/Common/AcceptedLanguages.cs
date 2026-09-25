using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

public class AcceptedLanguages
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguages(
            AcceptedLanguage,
            static language => language.ToString());

    [JsonPropertyName("acceptedLanguage")]
    public ICollection<AcceptedLanguage> AcceptedLanguage { get; set; } = [];
}
