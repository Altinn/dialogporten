using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;
using Microsoft.EntityFrameworkCore;

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

    public Task<DialogDocument?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.DialogDocuments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
