using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

// Numbered to match the server's domain enum (1-based) rather than the schema-generator's default 0-based
// numbering: an unset value must serialize as an unnamed number and fail the server's IsInEnum() check,
// rather than silently deserializing as the first named member.
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
