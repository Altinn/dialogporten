using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogStatusInput
{
    [EnumMember(Value = @"New")]
    New = 1,

    [EnumMember(Value = @"InProgress")]
    InProgress = 2,

    [EnumMember(Value = @"Draft")]
    Draft = 3,

    [EnumMember(Value = @"Sent")]
    Sent = 4,

    [EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 5,

    [EnumMember(Value = @"Completed")]
    Completed = 6,

    [EnumMember(Value = @"NotApplicable")]
    NotApplicable = 7,

    [EnumMember(Value = @"Awaiting")]
    Awaiting = 8,
}
