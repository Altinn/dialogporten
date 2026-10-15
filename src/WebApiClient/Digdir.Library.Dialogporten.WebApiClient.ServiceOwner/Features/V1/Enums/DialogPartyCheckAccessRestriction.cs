using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Numbered to match the server's domain enum (None = 1, DirectAndHierarchy = 2, ClientDelegation = 3), for
// the same reason as AuthorizationContextUnauthorizedPresentation: an unset value must fail the server's
// IsInEnum() check rather than silently deserializing as the least restrictive "None".
public enum DialogPartyCheckAccessRestriction
{
    [EnumMember(Value = @"None")]
    None = 1,

    [EnumMember(Value = @"DirectAndHierarchy")]
    DirectAndHierarchy = 2,

    [EnumMember(Value = @"ClientDelegation")]
    ClientDelegation = 3,
}
