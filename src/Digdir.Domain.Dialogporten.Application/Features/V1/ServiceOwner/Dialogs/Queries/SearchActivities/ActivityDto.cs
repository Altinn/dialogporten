using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchActivities;

public sealed class ActivityDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Uri? ExtendedType { get; set; }

    public DialogActivityType.Values Type { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? TransmissionId { get; set; }
}
