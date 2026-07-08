using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

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
