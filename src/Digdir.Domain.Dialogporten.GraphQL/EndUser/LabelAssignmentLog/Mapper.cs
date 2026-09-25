using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
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
        PerformedBy = MapActor(source.PerformedBy)
    };

    private static Actor MapActor(ActorDto source) => new()
    {
        ActorType = (ActorType)source.ActorType,
        ActorId = source.ActorId,
        ActorName = source.ActorName
    };
}
