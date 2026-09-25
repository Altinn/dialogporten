using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

// Numbered to match the server's domain enum (Disabled = 1, Excluded = 2), not the schema-generator's
// default 0-based numbering: an unset value must serialize as an unnamed number and fail the server's
// IsInEnum() check, rather than silently deserializing as a named (and less restrictive) "Disabled".
public enum AuthorizationContextUnauthorizedPresentation
{
    [EnumMember(Value = @"Disabled")]
    Disabled = 1,

    [EnumMember(Value = @"Excluded")]
    Excluded = 2,
}
