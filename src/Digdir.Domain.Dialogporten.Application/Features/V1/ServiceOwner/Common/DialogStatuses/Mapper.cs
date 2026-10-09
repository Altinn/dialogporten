using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;

[SuppressMessage("Style", "IDE0072:Add missing cases")]
internal static class DialogStatusInputMapExtensions
{
    extension(DialogStatusInput source)
    {
        internal DialogStatus.Values ToDialogStatusValue() => source switch
        {
            DialogStatusInput.New => DialogStatus.Values.NotApplicable,
            DialogStatusInput.Sent => DialogStatus.Values.Awaiting,
            _ => (DialogStatus.Values)source
        };
    }

    extension(DialogStatusInput? source)
    {
        internal DialogStatus.Values ToDialogStatusValue() =>
            (source ?? DialogStatusInput.NotApplicable)
            .ToDialogStatusValue();
    }

    extension(DialogStatus.Values source)
    {
        internal DialogStatusInput ToDialogStatusInput() => source switch
        {
            DialogStatus.Values.NotApplicable => DialogStatusInput.NotApplicable,
            DialogStatus.Values.InProgress => DialogStatusInput.InProgress,
            DialogStatus.Values.Draft => DialogStatusInput.Draft,
            DialogStatus.Values.Awaiting => DialogStatusInput.Awaiting,
            DialogStatus.Values.RequiresAttention => DialogStatusInput.RequiresAttention,
            DialogStatus.Values.Completed => DialogStatusInput.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
