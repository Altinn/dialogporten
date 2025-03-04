using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

public sealed class ActorDto
{
    /// <summary>
    /// The type of actor; either the service owner, or someone representing the party.
    /// </summary>
    public ActorType.Values ActorType { get; set; }

    /// <summary>
    /// The name of the actor.
    /// </summary>
    /// <example>Ola Nordmann</example>
    public string? ActorName { get; set; }

    /// <summary>
    /// The identifier (national identity number or organization number) of the actor.
    /// </summary>
    /// <example>urn:altinn:person:identifier-no:12018212345</example>
    public string? ActorId { get; set; }
}
