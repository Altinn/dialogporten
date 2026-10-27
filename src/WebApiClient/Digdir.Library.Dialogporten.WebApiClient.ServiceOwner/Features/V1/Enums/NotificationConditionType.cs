using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
public enum NotificationConditionType
{
    [EnumMember(Value = @"NotExists")]
    NotExists = 1,

    [EnumMember(Value = @"Exists")]
    Exists = 2,
}
