using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
public enum HttpVerb
{
    [EnumMember(Value = @"GET")]
    Get = 1,

    [EnumMember(Value = @"POST")]
    Post = 2,

    [EnumMember(Value = @"PUT")]
    Put = 3,

    [EnumMember(Value = @"PATCH")]
    Patch = 4,

    [EnumMember(Value = @"DELETE")]
    Delete = 5,

    [EnumMember(Value = @"HEAD")]
    Head = 6,

    [EnumMember(Value = @"OPTIONS")]
    Options = 7,

    [EnumMember(Value = @"TRACE")]
    Trace = 8,

    [EnumMember(Value = @"CONNECT")]
    Connect = 9,
}
