using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogStatus
{
    [EnumMember(Value = @"InProgress")]
    InProgress = 0,

    [EnumMember(Value = @"Draft")]
    Draft = 1,

    [EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 2,

    [EnumMember(Value = @"Completed")]
    Completed = 3,

    [EnumMember(Value = @"NotApplicable")]
    NotApplicable = 4,

    [EnumMember(Value = @"Awaiting")]
    Awaiting = 5,
}
