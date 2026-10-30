using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum DialogGuiActionPriority
{
    [EnumMember(Value = @"Primary")]
    Primary = 1,

    [EnumMember(Value = @"Secondary")]
    Secondary = 2,

    [EnumMember(Value = @"Tertiary")]
    Tertiary = 3,
}
