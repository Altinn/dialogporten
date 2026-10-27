using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Numbered to match the server's domain enum (1-based) rather than the schema-generator's default 0-based
// numbering: an unset value must serialize as an unnamed number and fail the server's IsInEnum() check,
// rather than silently deserializing as the first named member.
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
