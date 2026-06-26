using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Context;

public class CreateDialogServiceOwnerContext
{
    /// <summary>
    /// A list of labels, not visible in end-user APIs.
    /// </summary>
    [JsonPropertyName("serviceOwnerLabels")]
    public ICollection<ServiceOwnerLabel>? ServiceOwnerLabels { get; set; }
}