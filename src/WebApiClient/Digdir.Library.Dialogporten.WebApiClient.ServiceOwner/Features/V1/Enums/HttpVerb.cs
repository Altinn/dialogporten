using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
public enum HttpVerb
{
    [EnumMember(Value = @"GET")]
    GET = 1,

    [EnumMember(Value = @"POST")]
    POST = 2,

    [EnumMember(Value = @"PUT")]
    PUT = 3,

    [EnumMember(Value = @"PATCH")]
    PATCH = 4,

    [EnumMember(Value = @"DELETE")]
    DELETE = 5,

    [EnumMember(Value = @"HEAD")]
    HEAD = 6,

    [EnumMember(Value = @"OPTIONS")]
    OPTIONS = 7,

    [EnumMember(Value = @"TRACE")]
    TRACE = 8,

    [EnumMember(Value = @"CONNECT")]
    CONNECT = 9,
}
