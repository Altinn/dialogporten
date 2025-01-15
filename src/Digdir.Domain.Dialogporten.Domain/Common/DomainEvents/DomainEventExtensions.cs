using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public static class DomainEventExtensions
{
    public static bool ShouldNotBeSentToAltinn(this IDomainEvent domainEvent)
        => domainEvent.Metadata.TryGetValue(Constants.DisableAltinnEvents, out var value) && value == "true";
}
