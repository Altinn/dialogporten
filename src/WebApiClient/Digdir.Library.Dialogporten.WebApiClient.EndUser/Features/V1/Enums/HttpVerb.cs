using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum HttpVerb
{
    [EnumMember(Value = @"GET")]
    GET = 0,

    [EnumMember(Value = @"POST")]
    POST = 1,

    [EnumMember(Value = @"PUT")]
    PUT = 2,

    [EnumMember(Value = @"PATCH")]
    PATCH = 3,

    [EnumMember(Value = @"DELETE")]
    DELETE = 4,

    [EnumMember(Value = @"HEAD")]
    HEAD = 5,

    [EnumMember(Value = @"OPTIONS")]
    OPTIONS = 6,

    [EnumMember(Value = @"TRACE")]
    TRACE = 7,

    [EnumMember(Value = @"CONNECT")]
    CONNECT = 8,
}
