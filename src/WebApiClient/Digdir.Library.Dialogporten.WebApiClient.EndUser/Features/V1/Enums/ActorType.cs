using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum ActorType
{
    [EnumMember(Value = @"PartyRepresentative")]
    PartyRepresentative = 0,

    [EnumMember(Value = @"ServiceOwner")]
    ServiceOwner = 1,
}
