namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Action;

public enum HttpVerb
{
    [System.Runtime.Serialization.EnumMember(Value = @"GET")]
    Get = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"POST")]
    Post = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"PUT")]
    Put = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"PATCH")]
    Patch = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"DELETE")]
    Delete = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"HEAD")]
    Head = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"OPTIONS")]
    Options = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"TRACE")]
    Trace = 7,

    [System.Runtime.Serialization.EnumMember(Value = @"CONNECT")]
    Connect = 8,
}