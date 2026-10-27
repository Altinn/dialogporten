using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
public enum DialogStatus
{
    [EnumMember(Value = @"InProgress")]
    InProgress = 2,

    [EnumMember(Value = @"Draft")]
    Draft = 3,

    [EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 5,

    [EnumMember(Value = @"Completed")]
    Completed = 6,

    [EnumMember(Value = @"NotApplicable")]
    NotApplicable = 7,

    [EnumMember(Value = @"Awaiting")]
    Awaiting = 8,
}
