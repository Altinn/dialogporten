using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Notification;

public enum NotificationConditionType
{
    [System.Runtime.Serialization.EnumMember(Value = @"NotExists")]
    NotExists = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Exists")]
    Exists = 1,
}

public class NotificationCondition
{
    [JsonPropertyName("sendNotification")]
    public bool SendNotification { get; set; }
}
