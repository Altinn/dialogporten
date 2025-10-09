namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSearchRepository
{
    Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken);
}
