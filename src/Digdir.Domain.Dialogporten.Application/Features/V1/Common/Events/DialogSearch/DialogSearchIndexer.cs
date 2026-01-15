using AsyncKeyedLock;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.DialogSearch;

internal sealed class DialogSearchIndexer(IDialogSearchRepository db) : INotificationHandler<DialogCreatedDomainEvent>, INotificationHandler<DialogUpdatedDomainEvent>
{
    private static readonly AsyncKeyedLocker<string> Semaphore = new();
    private const string IndexerLockKey = "dialog-search-indexer";
    private readonly IDialogSearchRepository _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task Handle(DialogCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        UpdateIndex(notification.DialogId, cancellationToken);

    public Task Handle(DialogUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        UpdateIndex(notification.DialogId, cancellationToken);

    private async Task UpdateIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        using var _ = await Semaphore.LockAsync(IndexerLockKey, cancellationToken);
        await _db.UpsertFreeTextSearchIndex(dialogId, cancellationToken);
    }
}
