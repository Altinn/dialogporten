using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
public enum DialogTransmissionType
{
    [EnumMember(Value = @"Information")]
    Information = 1,

    [EnumMember(Value = @"Acceptance")]
    Acceptance = 2,

    [EnumMember(Value = @"Rejection")]
    Rejection = 3,

    [EnumMember(Value = @"Request")]
    Request = 4,

    [EnumMember(Value = @"Alert")]
    Alert = 5,

    [EnumMember(Value = @"Decision")]
    Decision = 6,

    [EnumMember(Value = @"Submission")]
    Submission = 7,

    [EnumMember(Value = @"Correction")]
    Correction = 8,
}
