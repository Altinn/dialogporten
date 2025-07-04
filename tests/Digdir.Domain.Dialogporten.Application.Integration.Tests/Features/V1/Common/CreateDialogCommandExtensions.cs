using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

internal static class CreateDialogCommandExtensions
{
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

    public static TransmissionDto AddAttachment(this TransmissionDto transmission, Action<TransmissionAttachmentDto>? modify = null)
    {
        var attachment = new TransmissionAttachmentDto
        {
            Id = NewUuidV7(),
            DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
            Urls = [new() { ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                Url = new Uri("https://example.com/file.pdf") }]
        };

        modify?.Invoke(attachment);
        transmission.Attachments.Add(attachment);
        return transmission;
    }
}
