using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

internal static class CreateDialogCommandExtensions
{
    extension(CreateDialogCommand command)
    {
        public CreateDialogCommand AddServiceOwnerLabels(params string[] labels)
        {
            command.Dto.ServiceOwnerContext ??= new DialogServiceOwnerContextDto();
            foreach (var label in labels)
            {
                command.Dto.ServiceOwnerContext.ServiceOwnerLabels.Add(new ServiceOwnerLabelDto { Value = label });
            }
            return command;
        }

        public CreateDialogCommand AddTransmission(Action<TransmissionDto>? modify = null)
        {
            var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
            modify?.Invoke(transmission);
            command.Dto.Transmissions.Add(transmission);
            return command;
        }
    }

    extension(TransmissionDto transmission)
    {
        public TransmissionDto WithPartyRepresentativeActor()
        {
            transmission.Sender = new ActorDto
            {
                ActorType = ActorType.Values.PartyRepresentative,
                ActorName = "Fredrik",
            };

            return transmission;
        }

        public TransmissionDto WithServiceOwnerActor()
        {
            transmission.Sender = new ActorDto
            {
                ActorType = ActorType.Values.ServiceOwner,
            };

            return transmission;
        }

        public TransmissionDto AddAttachment(Action<TransmissionAttachmentDto>? modify = null)
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

    extension(CreateDialogCommand command)
    {
        public CreateDialogCommand AddActivity(DialogActivityType.Values? type = null, Action<ActivityDto>? modify = null)
        {
            var activity = DialogGenerator.GenerateFakeDialogActivity(type);
            modify?.Invoke(activity);
            command.Dto.Activities.Add(activity);
            return command;
        }

        public CreateDialogCommand AddAttachment(Action<AttachmentDto>? modify = null)
        {
            var attachment = DialogGenerator.GenerateFakeDialogAttachment();
            modify?.Invoke(attachment);
            command.Dto.Attachments.Add(attachment);
            return command;
        }
    }
}
