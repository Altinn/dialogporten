using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public partial class NotificationCondition
{
    [JsonPropertyName("sendNotification")]
    public bool SendNotification { get; set; }
}
