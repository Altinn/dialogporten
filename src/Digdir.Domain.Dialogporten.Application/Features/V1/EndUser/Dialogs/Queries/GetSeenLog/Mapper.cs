using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;

internal static class SeenLogMapper
{
    extension(DialogSeenLog source)
    {
        public SeenLogDto ToDto() => new()
        {
            Id = source.Id,
            SeenAt = source.CreatedAt,
            SeenBy = source.SeenBy.ToDto(),
            IsViaServiceOwner = source.IsViaServiceOwner
        };
    }
}
