using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

internal static class LabelAssignmentLogMapper
{
    extension(LabelAssignmentLog source)
    {
        public LabelAssignmentLogDto ToDto() => new()
        {
            CreatedAt = source.CreatedAt,
            Name = source.Name,
            Action = source.Action,
            PerformedBy = source.PerformedBy.ToDto()
        };
    }
}
