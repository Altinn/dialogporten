using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class LabelAssignmentLogDto
{
    /// <summary>
    /// The date and time when the label was set or removed.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The name of the label.
    /// </summary>
    /// <example>archive</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The action performed on the label, either "set" or "remove".
    /// </summary>
    /// <example>set</example>
    public string Action { get; set; } = null!;

    /// <summary>
    /// The actor that performed the label change.
    /// </summary>
    public ActorDto PerformedBy { get; set; } = null!;
}
