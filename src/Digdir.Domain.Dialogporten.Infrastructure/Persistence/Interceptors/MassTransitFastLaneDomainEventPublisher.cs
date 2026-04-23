using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed partial class MassTransitFastLaneDomainEventPublisher(
    Lazy<IBus> bus,
    ILogger<MassTransitFastLaneDomainEventPublisher> logger) : IFastLaneDomainEventPublisher
{
    private static readonly TimeSpan FastLanePublishTimeout = TimeSpan.FromMilliseconds(500);

    public async Task<bool> TryPublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(FastLanePublishTimeout);
        var publishToken = timeoutCts.Token;

        try
        {
            await Task.WhenAll(domainEvents
                .Select(x => bus.Value.Publish(x, x.GetType(), publishToken)));

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            LogFastLaneFallback(logger, "timed out", domainEvents.Count);
            return false;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Fast-lane publish failed for {EventCount} domain events, falling back to the outbox.", domainEvents.Count);
            return false;
        }
    }

    [LoggerMessage(LogLevel.Information, "Fast-lane publish {Reason} for {EventCount} domain events, falling back to the outbox.")]
    private static partial void LogFastLaneFallback(ILogger logger, string Reason, int EventCount);
}
