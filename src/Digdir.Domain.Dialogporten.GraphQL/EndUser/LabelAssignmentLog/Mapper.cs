using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using LabelAssignmentLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog.LabelAssignmentLogDto;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.LabelAssignmentLog;

internal static class LabelAssignmentLogMapExtensions
{
    extension(List<LabelAssignmentLogDto> source)
    {
        public List<LabelAssignmentLog> ToLabelAssignmentLogs() =>
            source.Select(MapLabelAssignmentLog).ToList();
    }

    private static LabelAssignmentLog MapLabelAssignmentLog(LabelAssignmentLogDto source) => new()
    {
        CreatedAt = source.CreatedAt,
        Name = source.Name,
        Action = source.Action,
        PerformedBy = source.PerformedBy.ToGraphQlActor()
    };
}
