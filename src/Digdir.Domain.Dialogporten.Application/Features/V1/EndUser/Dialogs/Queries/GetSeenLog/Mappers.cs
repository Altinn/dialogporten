using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;

internal static class DialogSeenLogMapExtensions
{
    extension(DialogSeenLog seenLog)
    {
        internal SeenLogDto ToDto() => new()
        {
            Id = seenLog.Id,
            SeenAt = seenLog.CreatedAt,
            SeenBy = seenLog.SeenBy.ToDto(),
            IsViaServiceOwner = seenLog.IsViaServiceOwner
        };
    }
}
