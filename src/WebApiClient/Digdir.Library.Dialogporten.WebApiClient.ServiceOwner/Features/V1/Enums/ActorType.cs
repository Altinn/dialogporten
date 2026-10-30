using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum ActorType
{
    [EnumMember(Value = @"PartyRepresentative")]
    PartyRepresentative = 1,

    [EnumMember(Value = @"ServiceOwner")]
    ServiceOwner = 2,
}
