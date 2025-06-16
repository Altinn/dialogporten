using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogDocumentRepository
{
    Task Add(DialogDocument document, CancellationToken cancellationToken = default);
    Task<DialogDocument?> Get(Guid id, CancellationToken cancellationToken = default);
}
