using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Numbered to match the server's domain enum (1-based) rather than the schema-generator's default 0-based
// numbering: an unset value must serialize as an unnamed number and fail the server's IsInEnum() check,
// rather than silently deserializing as the first named member.
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
