using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

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
