using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Actors;

public enum ActorType
{
    [System.Runtime.Serialization.EnumMember(Value = @"PartyRepresentative")]
    PartyRepresentative = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"ServiceOwner")]
    ServiceOwner = 1,
}

public class Actor
{
    /// <summary>
    /// The type of actor; either the service owner, or someone representing the party.
    /// </summary>
    [JsonPropertyName("actorType")]
    [JsonConverter(typeof(JsonStringEnumConverter<ActorType>))]
    public ActorType ActorType { get; set; }

    /// <summary>
    /// The name of the actor.
    /// </summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; set; }

    /// <summary>
    /// The identifier (national identity number or organization number) of the actor.
    /// </summary>
    [JsonPropertyName("actorId")]
    public string? ActorId { get; set; }
}
