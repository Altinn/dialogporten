using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

internal static class CreateDialogCommandExtensions
{
    public static CreateDialogCommand AddServiceOwnerLabels(this CreateDialogCommand command, params string[] labels)
    {
        command.Dto.ServiceOwnerContext ??= new DialogServiceOwnerContextDto();
        foreach (var label in labels)
        {
            command.Dto.ServiceOwnerContext.ServiceOwnerLabels.Add(new ServiceOwnerLabelDto { Value = label });
        }
        return command;
    }

    public static CreateDialogCommand AddTransmission(this CreateDialogCommand command, Action<TransmissionDto>? modify = null)
    {
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
        modify?.Invoke(transmission);
        command.Dto.Transmissions.Add(transmission);
        return command;
    }

    public static CreateDialogCommand AddActivity(this CreateDialogCommand command, DialogActivityType.Values? type = null, Action<ActivityDto>? modify = null)
    {
        var activity = DialogGenerator.GenerateFakeDialogActivity(type);
        modify?.Invoke(activity);
        command.Dto.Activities.Add(activity);
        return command;
    }

    public static CreateDialogCommand AddAttachment(this CreateDialogCommand command, Action<AttachmentDto>? modify = null)
    {
        var attachment = DialogGenerator.GenerateFakeDialogAttachment();
        modify?.Invoke(attachment);
        command.Dto.Attachments.Add(attachment);
        return command;
    }
}
