using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common;

internal static class ValidationErrorStrings
{
    internal const string PropertyNameMustBeLessThanOrEqualToComparisonProperty =
        "'{PropertyName}' must be less than or equal to '{ComparisonProperty}'.";

    internal static readonly string SentLabelNotAllowed =
        $"Cannot manually add or remove system label {SystemLabel.Values.Sent}. " +
        $"It is added automatically when a " +
        $"transmissions of type '{DialogTransmissionType.Values.Submission} or " +
        $"'{DialogTransmissionType.Values.Correction}' is added to the dialog.";
}
