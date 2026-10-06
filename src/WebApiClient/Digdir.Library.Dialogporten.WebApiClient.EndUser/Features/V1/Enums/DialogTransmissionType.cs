using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum DialogTransmissionType
{
    [EnumMember(Value = @"Information")]
    Information = 0,

    [EnumMember(Value = @"Acceptance")]
    Acceptance = 1,

    [EnumMember(Value = @"Rejection")]
    Rejection = 2,

    [EnumMember(Value = @"Request")]
    Request = 3,

    [EnumMember(Value = @"Alert")]
    Alert = 4,

    [EnumMember(Value = @"Decision")]
    Decision = 5,

    [EnumMember(Value = @"Submission")]
    Submission = 6,

    [EnumMember(Value = @"Correction")]
    Correction = 7,
}
