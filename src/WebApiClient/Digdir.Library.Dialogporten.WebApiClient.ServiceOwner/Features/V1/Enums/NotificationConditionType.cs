using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum NotificationConditionType
{
    [EnumMember(Value = @"NotExists")]
    NotExists = 0,

    [EnumMember(Value = @"Exists")]
    Exists = 1,
}
