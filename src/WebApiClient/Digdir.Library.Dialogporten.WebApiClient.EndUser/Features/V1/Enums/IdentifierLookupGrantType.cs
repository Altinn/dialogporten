using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

// Values start at 1 on purpose: an unset (default) value is then not a valid member and gets rejected,
// instead of silently being sent as the first member.
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
