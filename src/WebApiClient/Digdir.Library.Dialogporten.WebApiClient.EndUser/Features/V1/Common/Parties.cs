using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

public class Parties
{
    [JsonPropertyName("authorizedParties")]
    public ICollection<AuthorizedParty>? AuthorizedParties { get; set; } = [];
}
