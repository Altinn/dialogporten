using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Name-based conversions between the output status enum (<see cref="DialogStatus"/>) and the
/// input status enum (<see cref="DialogStatusInput"/>). The two enums do not share ordinal values,
/// so callers must use these helpers rather than casting.
/// </summary>
public static class DialogStatusMapping
{
    /// <summary>
    /// Converts an output <see cref="DialogStatus"/> to the corresponding input <see cref="DialogStatusInput"/>.
    /// Every value returned by the API maps one-to-one; <see cref="DialogStatusInput.New"/> and
    /// <see cref="DialogStatusInput.Sent"/> are input-only and therefore never produced here.
    /// </summary>
    public static DialogStatusInput ToDialogStatusInput(this DialogStatus status) => status switch
    {
        DialogStatus.InProgress => DialogStatusInput.InProgress,
        DialogStatus.Draft => DialogStatusInput.Draft,
        DialogStatus.RequiresAttention => DialogStatusInput.RequiresAttention,
        DialogStatus.Completed => DialogStatusInput.Completed,
        DialogStatus.NotApplicable => DialogStatusInput.NotApplicable,
        DialogStatus.Awaiting => DialogStatusInput.Awaiting,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown dialog status."),
    };

    /// <summary>
    /// Converts an input <see cref="DialogStatusInput"/> to the corresponding output <see cref="DialogStatus"/>.
    /// The input-only values are mapped to their nearest output equivalent:
    /// <see cref="DialogStatusInput.New"/> becomes <see cref="DialogStatus.NotApplicable"/> and
    /// <see cref="DialogStatusInput.Sent"/> becomes <see cref="DialogStatus.Awaiting"/>.
    /// </summary>
    public static DialogStatus ToDialogStatus(this DialogStatusInput status) => status switch
    {
        DialogStatusInput.New => DialogStatus.NotApplicable,
        DialogStatusInput.InProgress => DialogStatus.InProgress,
        DialogStatusInput.Draft => DialogStatus.Draft,
        DialogStatusInput.Sent => DialogStatus.Awaiting,
        DialogStatusInput.RequiresAttention => DialogStatus.RequiresAttention,
        DialogStatusInput.Completed => DialogStatus.Completed,
        DialogStatusInput.NotApplicable => DialogStatus.NotApplicable,
        DialogStatusInput.Awaiting => DialogStatus.Awaiting,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown dialog status."),
    };
}
