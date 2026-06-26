namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public enum DialogStatus
{
    [System.Runtime.Serialization.EnumMember(Value = @"InProgress")]
    InProgress = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Draft")]
    Draft = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Completed")]
    Completed = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"NotApplicable")]
    NotApplicable = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Awaiting")]
    Awaiting = 5,
}

public enum DialogStatusInput
{
    [System.Runtime.Serialization.EnumMember(Value = @"New")]
    New = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"InProgress")]
    InProgress = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Draft")]
    Draft = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Sent")]
    Sent = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"RequiresAttention")]
    RequiresAttention = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Completed")]
    Completed = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"NotApplicable")]
    NotApplicable = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"Awaiting")]
    Awaiting = 7,
}
