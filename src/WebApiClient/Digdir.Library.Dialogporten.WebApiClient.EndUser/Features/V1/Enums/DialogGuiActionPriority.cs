using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

// Numbered to match the server's domain enum (1-based) rather than the schema-generator's default 0-based
// numbering: an unset value must serialize as an unnamed number and fail the server's IsInEnum() check,
// rather than silently deserializing as the first named member.
public enum DialogGuiActionPriority
{
    [EnumMember(Value = @"Primary")]
    Primary = 1,

    [EnumMember(Value = @"Secondary")]
    Secondary = 2,

    [EnumMember(Value = @"Tertiary")]
    Tertiary = 3,
}
