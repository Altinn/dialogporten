using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Numbered to match the server's domain enum (1-based) rather than the schema-generator's default 0-based
// numbering: an unset value must serialize as an unnamed number and fail the server's IsInEnum() check,
// rather than silently deserializing as the first named member.
public enum SystemLabel
{
    [EnumMember(Value = @"Default")]
    Default = 1,

    [EnumMember(Value = @"Bin")]
    Bin = 2,

    [EnumMember(Value = @"Archive")]
    Archive = 3,

    [EnumMember(Value = @"MarkedAsUnopened")]
    MarkedAsUnopened = 4,

    [EnumMember(Value = @"Sent")]
    Sent = 5,
}
