using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum AttachmentUrlConsumerType
{
    [EnumMember(Value = @"Gui")]
    Gui = 0,

    [EnumMember(Value = @"Api")]
    Api = 1,
}
