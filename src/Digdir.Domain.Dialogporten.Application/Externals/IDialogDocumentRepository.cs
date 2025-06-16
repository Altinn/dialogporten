using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;
using System.Linq.Expressions;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogDocumentRepository
{
    Task Add(DialogDocument document, CancellationToken cancellationToken = default);

    Task Update(DialogDocument document, CancellationToken cancellationToken = default);

    Task Remove(DialogDocument document, CancellationToken cancellationToken = default);

    Task<DialogDocument?> Get(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DialogDocument>> Find(
        Expression<Func<DialogDocument, bool>> predicate,
        CancellationToken cancellationToken = default);
}
