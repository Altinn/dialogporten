using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum SystemLabel
{
    [EnumMember(Value = @"Default")]
    Default = 0,

    [EnumMember(Value = @"Bin")]
    Bin = 1,

    [EnumMember(Value = @"Archive")]
    Archive = 2,

    [EnumMember(Value = @"MarkedAsUnopened")]
    MarkedAsUnopened = 3,

    [EnumMember(Value = @"Sent")]
    Sent = 4,
}
