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
}
