using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.LabelAssignmentLogs;

public sealed class LabelAssignmentGdprExtractDto
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required string Name { get; set; }
    public required string Action { get; set; }
    public required ActorDto PerformedBy { get; set; } = null!;
    public required Guid? DialogId { get; set; }
}
