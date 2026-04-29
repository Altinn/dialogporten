using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSeenLogWriter
{
    Task<DialogSeenLogWriteResult> EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        string actorId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken);
}

public sealed record DialogSeenLogWriteResult(
    Guid ActorNameId,
    string ActorId,
    string ActorName);
