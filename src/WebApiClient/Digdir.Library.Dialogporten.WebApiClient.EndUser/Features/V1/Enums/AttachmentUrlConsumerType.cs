using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum AttachmentUrlConsumerType
{
    [EnumMember(Value = @"Gui")]
    Gui = 1,

    [EnumMember(Value = @"Api")]
    Api = 2,
}
