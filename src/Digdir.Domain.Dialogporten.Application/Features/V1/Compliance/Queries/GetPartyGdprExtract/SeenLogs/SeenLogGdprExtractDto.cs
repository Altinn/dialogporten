using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.SeenLogs;

public sealed class SeenLogGdprExtractDto
{
    public required Guid Id { get; set; }
    public required DateTimeOffset SeenAt { get; set; }

    public required ActorDto SeenBy { get; set; } = null!;

    public required bool? IsViaServiceOwner { get; set; }
    public required Guid DialogId { get; set; }
}
