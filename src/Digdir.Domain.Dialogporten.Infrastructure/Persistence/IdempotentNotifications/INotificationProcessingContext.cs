using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;

public interface INotificationProcessingContext : IAsyncDisposable
{
    Task Ack(CancellationToken cancellationToken = default);
    Task Nack(CancellationToken cancellationToken = default);
    Task AckHandler(string handlerName, CancellationToken cancellationToken = default);
    Task<bool> HandlerIsAcked(string handlerName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationProcessingContext : INotificationProcessingContext
{
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Action<Guid> _onDispose;
    private readonly Guid _eventId;

    private ILogger<NotificationProcessingContext>? _logger;
    private DialogDbContext? _db;
    private IServiceScope? _serviceScope;
    private bool _acknowledged;

    public bool Disposed { get; private set; }

    public NotificationProcessingContext(
        IServiceScopeFactory serviceScopeFactory,
        Guid eventId,
        Action<Guid> onDispose)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        _eventId = eventId;
    }

    public async Task Ack(CancellationToken cancellationToken = default)
    {
        EnsureAlive();
        var existingAckInDatabase = _db.ChangeTracker
            .Entries<NotificationAcknowledgement>()
            .Any(x => x.Entity.EventId == _eventId && x.State
                is EntityState.Unchanged
                or EntityState.Modified);

        if (existingAckInDatabase)
        {
            await _db.NotificationAcknowledgements
                .Where(x => x.EventId == _eventId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _acknowledged = true;
    }

    public async Task Nack(CancellationToken cancellationToken = default)
    {
        EnsureAlive();
        if (!_acknowledged && _db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task AckHandler(string handlerName, CancellationToken cancellationToken = default)
    {
        EnsureAlive();
        _db.NotificationAcknowledgements.Add(new()
        {
            EventId = _eventId,
            NotificationHandler = handlerName
        });

        return Task.CompletedTask;
    }

    public Task<bool> HandlerIsAcked(string handlerName, CancellationToken cancellationToken = default)
    {
        EnsureAlive();
        var acknowledged = _db.NotificationAcknowledgements
            .Local
            .Any(x => x.EventId == _eventId && x.NotificationHandler == handlerName);
        return Task.FromResult(acknowledged);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_acknowledged)
        {
            try
            {
                await Nack();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to save changes to database.");
            }
        }

        _initializeLock.Dispose();
        _serviceScope?.Dispose();
        _serviceScope = null;
        _db = null;
        _onDispose(_eventId);
        Disposed = true;
    }

    internal async Task Initialize(bool isFirstAttempt = false, CancellationToken cancellationToken = default)
    {
        await _initializeLock.WaitAsync(cancellationToken);

        try
        {
            if (_serviceScope is not null)
            {
                throw new InvalidOperationException("Transaction already started.");
            }

            _serviceScope = _serviceScopeFactory.CreateScope();
            _db = _serviceScope.ServiceProvider.GetRequiredService<DialogDbContext>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<NotificationProcessingContext>>();
            if (!isFirstAttempt)
            {
                await _db.NotificationAcknowledgements
                    .Where(x => x.EventId == _eventId)
                    .LoadAsync(cancellationToken);
            }
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    [MemberNotNull(nameof(_db), nameof(_serviceScope), nameof(_logger))]
    private void EnsureAlive()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        if (_db is null || _serviceScope is null || _logger is null)
        {
            throw new InvalidOperationException("Transaction not initialized.");
        }
    }
}
