#pragma warning disable CS0618

using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;

internal static class DialogStatusMapper
{
    extension(DialogStatusInput source)
    {
        public DialogStatus.Values ToEntity() => source switch
        {
            DialogStatusInput.New => DialogStatus.Values.NotApplicable,
            DialogStatusInput.Sent => DialogStatus.Values.Awaiting,
            DialogStatusInput.NotApplicable => DialogStatus.Values.NotApplicable,
            DialogStatusInput.InProgress => DialogStatus.Values.InProgress,
            DialogStatusInput.Draft => DialogStatus.Values.Draft,
            DialogStatusInput.Awaiting => DialogStatus.Values.Awaiting,
            DialogStatusInput.RequiresAttention => DialogStatus.Values.RequiresAttention,
            DialogStatusInput.Completed => DialogStatus.Values.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
