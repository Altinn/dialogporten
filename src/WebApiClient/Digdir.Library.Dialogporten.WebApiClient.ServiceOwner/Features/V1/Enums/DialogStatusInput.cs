using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogStatusInput
{
    [EnumMember(Value = @"New")]
    New = 0,

    [EnumMember(Value = @"InProgress")]
    InProgress = 1,

    [EnumMember(Value = @"Draft")]
    Draft = 2,

    [EnumMember(Value = @"Sent")]
    Sent = 3,

    [EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 4,

    [EnumMember(Value = @"Completed")]
    Completed = 5,

    [EnumMember(Value = @"NotApplicable")]
    NotApplicable = 6,

    [EnumMember(Value = @"Awaiting")]
    Awaiting = 7,
}
