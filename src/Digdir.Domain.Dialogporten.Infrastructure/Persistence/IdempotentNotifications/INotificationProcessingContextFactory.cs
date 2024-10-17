using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;

public interface INotificationProcessingContextFactory
{
    Task<INotificationProcessingContext> CreateContext(IDomainEvent domainEvent, bool isFirstAttempt = false, CancellationToken cancellationToken = default);
    INotificationProcessingContext GetExistingContext(Guid eventId);
}

internal sealed class NotificationProcessingContextFactory : INotificationProcessingContextFactory, IDisposable
{
    private readonly ConcurrentDictionary<Guid, WeakReference<NotificationProcessingContext>> _contextByEventId = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationProcessingContextFactory> _logger;
    private readonly PeriodicTimer _cleanupTimer = new(TimeSpan.FromMinutes(10));
    private readonly CancellationTokenSource _cleanupCts = new();

    public NotificationProcessingContextFactory(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationProcessingContextFactory> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Task.Run(ContextHousekeeping);
    }

    public async Task<INotificationProcessingContext> CreateContext(
        IDomainEvent domainEvent,
        bool isFirstAttempt = false,
        CancellationToken cancellationToken = default)
    {
        var transaction = GetOrAddContext(domainEvent.EventId);
        try
        {
            await transaction.Initialize(isFirstAttempt, cancellationToken);
            return transaction;
        }
        catch (Exception)
        {
            RemoveContext(domainEvent.EventId);
            throw;
        }
    }

    public INotificationProcessingContext GetExistingContext(Guid eventId)
    {
        return _contextByEventId.TryGetValue(eventId, out var weakContext)
            && TryGetLiveContext(weakContext, out var context)
                ? context
                : throw new InvalidOperationException("Notification context not found.");
    }

    public void Dispose()
    {
        _cleanupCts.Cancel();
        _cleanupCts.Dispose();
        _cleanupTimer.Dispose();
    }

    private NotificationProcessingContext GetOrAddContext(Guid eventId)
    {
        var weakContext = _contextByEventId.AddOrUpdate(eventId,
            addValueFactory: eventId => new(new(_serviceScopeFactory, eventId, onDispose: RemoveContext)),
            // Should the context, for whatever reason, be garbage collected or
            // disposed but still remain in the dictionary, we should recreate it.
            updateValueFactory: (eventId, old) => TryGetLiveContext(old, out _) ? old
                : new(new(_serviceScopeFactory, eventId, onDispose: RemoveContext)));

        return TryGetLiveContext(weakContext, out var context) ? context
            : throw new UnreachableException("The context should be alive at this point in time.");
    }

    private void RemoveContext(Guid eventId) => _contextByEventId.TryRemove(eventId, out _);

    private async Task ContextHousekeeping()
    {
        try
        {
            while (await _cleanupTimer.WaitForNextTickAsync(_cleanupCts.Token))
            {
                foreach (var key in _contextByEventId.Keys)
                {
                    if (_contextByEventId.TryGetValue(key, out var weakContext)
                        && !TryGetLiveContext(weakContext, out _))
                    {
                        RemoveContext(key);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An unhandled exception occurred in the notification processing context cleanup task. This may lead to memory leaks.");
        }
    }

    private static bool TryGetLiveContext(
        WeakReference<NotificationProcessingContext> weakContext,
        [NotNullWhen(true)] out NotificationProcessingContext? context)
        => weakContext.TryGetTarget(out context) && !context.Disposed;
}
