using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum JsonPatchOperationType
{
    [EnumMember(Value = @"Add")]
    Add = 0,

    [EnumMember(Value = @"Remove")]
    Remove = 1,

    [EnumMember(Value = @"Replace")]
    Replace = 2,

    [EnumMember(Value = @"Move")]
    Move = 3,

    [EnumMember(Value = @"Copy")]
    Copy = 4,

    [EnumMember(Value = @"Test")]
    Test = 5,

    [EnumMember(Value = @"Invalid")]
    Invalid = 6,
}
