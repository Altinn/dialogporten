using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public static class DomainEventExtensions
{
    public static bool ShouldNotBeSentToAltinnEvents(this IDomainEvent domainEvent)
        => domainEvent.Metadata.TryGetValue(Constants.IsSilentUpdate, out var value)
           && value == bool.TrueString;
}
