using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum IdentifierLookupGrantType
{
    [EnumMember(Value = @"Role")]
    Role = 1,

    [EnumMember(Value = @"AccessPackage")]
    AccessPackage = 2,

    [EnumMember(Value = @"ResourceDelegation")]
    ResourceDelegation = 3,

    [EnumMember(Value = @"InstanceDelegation")]
    InstanceDelegation = 4,
}
