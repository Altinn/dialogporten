using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum HttpVerb
{
    [EnumMember(Value = @"GET")]
    Get = 0,

    [EnumMember(Value = @"POST")]
    Post = 1,

    [EnumMember(Value = @"PUT")]
    Put = 2,

    [EnumMember(Value = @"PATCH")]
    Patch = 3,

    [EnumMember(Value = @"DELETE")]
    Delete = 4,

    [EnumMember(Value = @"HEAD")]
    Head = 5,

    [EnumMember(Value = @"OPTIONS")]
    Options = 6,

    [EnumMember(Value = @"TRACE")]
    Trace = 7,

    [EnumMember(Value = @"CONNECT")]
    Connect = 8,
}
