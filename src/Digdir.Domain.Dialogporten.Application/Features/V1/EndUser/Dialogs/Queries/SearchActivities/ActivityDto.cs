using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchActivities;

public sealed class ActivityDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Uri? ExtendedType { get; set; }
    public string? SeenByEndUserIdHash { get; set; }

    public DialogActivityType.Values Type { get; set; }

    public Guid? TransmissionId { get; set; }
}
