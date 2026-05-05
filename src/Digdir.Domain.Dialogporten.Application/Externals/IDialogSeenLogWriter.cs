using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSeenLogWriter
{
    Task<DialogSeenResult?> OnSeen(DialogEntity dialog, UserId userId, CancellationToken cancellationToken);
}

public sealed record DialogSeenResult(
    DialogSeenLog? NewSeenLog,
    bool IsContentSeen
);
