using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
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
