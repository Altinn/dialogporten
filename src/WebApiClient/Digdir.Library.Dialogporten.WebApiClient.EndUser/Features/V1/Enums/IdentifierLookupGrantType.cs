using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum IdentifierLookupGrantType
{
    [EnumMember(Value = @"Role")]
    Role = 0,

    [EnumMember(Value = @"AccessPackage")]
    AccessPackage = 1,

    [EnumMember(Value = @"ResourceDelegation")]
    ResourceDelegation = 2,

    [EnumMember(Value = @"InstanceDelegation")]
    InstanceDelegation = 3,
}
