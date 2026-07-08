using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateDialogServiceOwnerContext
{
    /// <summary>
    /// A list of labels, not visible in end-user APIs.
    /// </summary>
    [JsonPropertyName("serviceOwnerLabels")]
    public ICollection<CreateDialogServiceOwnerLabel>? ServiceOwnerLabels { get; set; }
}
