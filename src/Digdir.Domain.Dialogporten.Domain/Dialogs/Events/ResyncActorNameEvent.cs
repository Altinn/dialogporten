using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

/// <summary>
/// Used to re-sync actor names after the party name registry fails to answer, or returns no results for any reason.
///
/// Note that system users synchronize periodically in the party name registry.
/// This process may take up to 1 minute + processing time.
///
/// Ideally, we want to see none of these events in ASB.
/// </summary>
public sealed record ResyncActorNameEvent : DomainEvent
{
    public Guid ActorNameId { get; }
    public string Reason { get; }

    public bool DisableUpdateableFilter { get; }

    public ResyncActorNameEvent(
        Guid actorNameId,
        string reason,
        bool disableUpdateableFilter = false)
    {
        ActorNameId = actorNameId;
        Reason = reason;
        DisableUpdateableFilter = disableUpdateableFilter;
        EventId = actorNameId;
    }
}
