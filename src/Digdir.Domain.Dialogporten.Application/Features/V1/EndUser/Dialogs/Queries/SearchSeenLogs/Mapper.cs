using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;

internal static class SeenLogMapExtensions
{
    extension(DialogSeenLog source)
    {
        internal SeenLogDto ToDto() => new()
        {
            Id = source.Id,
            SeenAt = source.CreatedAt,
            SeenBy = source.SeenBy.ToDto(),
            IsViaServiceOwner = source.IsViaServiceOwner
        };
    }
}
