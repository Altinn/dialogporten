using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.Activities;

public sealed class ActivityGdprExtractDto
{
    public required Guid Id { get; set; }
    public required DateTimeOffset? CreatedAt { get; set; }
    public required Uri? ExtendedType { get; set; }
    public required DialogActivityType.Values Type { get; set; }
    public required Guid? TransmissionId { get; set; }
    public required ActorDto PerformedBy { get; set; } = null!;
    public required List<LocalizationDto> Description { get; set; } = [];
    public required Guid DialogId { get; set; }
}
