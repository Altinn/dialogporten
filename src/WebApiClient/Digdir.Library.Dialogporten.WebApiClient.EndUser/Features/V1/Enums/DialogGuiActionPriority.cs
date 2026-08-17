using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum DialogGuiActionPriority
{
    [EnumMember(Value = @"Primary")]
    Primary = 0,

    [EnumMember(Value = @"Secondary")]
    Secondary = 1,

    [EnumMember(Value = @"Tertiary")]
    Tertiary = 2,
}
