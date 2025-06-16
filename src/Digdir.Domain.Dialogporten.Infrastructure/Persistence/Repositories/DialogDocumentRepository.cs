using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogDocumentRepository : IDialogDocumentRepository
{
    private readonly DialogDbContext _db;

    public DialogDocumentRepository(DialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task Add(DialogDocument document, CancellationToken cancellationToken = default)
    {
        return _db.DialogDocuments.AddAsync(document, cancellationToken).AsTask();
    }

    public Task Update(DialogDocument document, CancellationToken cancellationToken = default)
    {
        _db.DialogDocuments.Update(document);
        return Task.CompletedTask;
    }

    public Task Remove(DialogDocument document, CancellationToken cancellationToken = default)
    {
        _db.DialogDocuments.Remove(document);
        return Task.CompletedTask;
    }

    public Task<DialogDocument?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.DialogDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<IReadOnlyCollection<DialogDocument>> Find(
        Expression<Func<DialogDocument, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _db.DialogDocuments
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyCollection<DialogDocument>)t.Result, cancellationToken);
    }
}
