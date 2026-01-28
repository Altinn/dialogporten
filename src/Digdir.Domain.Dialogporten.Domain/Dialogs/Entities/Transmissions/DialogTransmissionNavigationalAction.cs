using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

public sealed class DialogTransmissionNavigationalAction : IEntity, IImmutableEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Uri Url { get; set; } = null!;
    public DateTimeOffset? ExpiresAt { get; set; }

    // === Dependent relationships ===
    public Guid TransmissionId { get; set; }
    public DialogTransmission Transmission { get; set; } = null!;

    // === Principal relationships ===
    [AggregateChild]
    public DialogTransmissionNavigationalActionTitle Title { get; set; } = null!;
}

public sealed class DialogTransmissionNavigationalActionTitle : LocalizationSet
{
    public Guid NavigationalActionId { get; set; }
    public DialogTransmissionNavigationalAction NavigationalAction { get; set; } = null!;
}
