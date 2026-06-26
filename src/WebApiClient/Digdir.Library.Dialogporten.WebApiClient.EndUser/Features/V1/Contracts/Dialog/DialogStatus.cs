namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Dialog;

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