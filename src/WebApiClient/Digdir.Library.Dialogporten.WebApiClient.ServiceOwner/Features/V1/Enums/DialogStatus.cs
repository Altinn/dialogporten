using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogStatus
{
    [EnumMember(Value = @"InProgress")]
    InProgress = 1,

    [EnumMember(Value = @"Draft")]
    Draft = 2,

    [EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 3,

    [EnumMember(Value = @"Completed")]
    Completed = 4,

    [EnumMember(Value = @"NotApplicable")]
    NotApplicable = 5,

    [EnumMember(Value = @"Awaiting")]
    Awaiting = 6,
}
