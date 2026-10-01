using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.SeenLogs;

internal static class DialogSeenLogMapExtensions
{
    extension(DialogSeenLog seenLog)
    {
        internal SeenLogGdprExtractDto ToGdprExtractDto() => new()
        {
            Id = seenLog.Id,
            SeenAt = seenLog.CreatedAt,
            SeenBy = seenLog.SeenBy.ToDto(),
            IsViaServiceOwner = seenLog.IsViaServiceOwner,
            DialogId = seenLog.DialogId,
        };
    }
}
