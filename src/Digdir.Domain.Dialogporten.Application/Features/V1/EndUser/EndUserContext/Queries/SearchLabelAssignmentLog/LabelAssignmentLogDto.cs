using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class LabelAssignmentLogDto
{
    public DateTimeOffset CreatedAt { get; set; }

    public string Name { get; set; } = null!;

    public string Action { get; set; } = null!;

    public ActorDto PerformedBy { get; set; } = null!;

}
