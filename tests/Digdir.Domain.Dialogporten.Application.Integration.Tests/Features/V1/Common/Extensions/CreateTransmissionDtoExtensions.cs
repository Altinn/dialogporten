using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

internal static class CreateTransmissionDtoExtensions
{
    extension(CreateTransmissionDto transmission)
    {
        public CreateTransmissionDto AddNavigationalAction(Action<TransmissionNavigationalActionDto>? modify = null)
        {
            var action = new TransmissionNavigationalActionDto
            {
                Title = DialogGenerator.GenerateFakeLocalizations(1),
                Url = new Uri("https://digdir.no/action")
            };

            modify?.Invoke(action);
            transmission.NavigationalActions.Add(action);

            return transmission;
        }

        public CreateTransmissionDto AddAttachment(Action<TransmissionAttachmentDto>? modify = null)
        {
            var attachment = new TransmissionAttachmentDto
            {
                DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
                Urls =
                [
                    new TransmissionAttachmentUrlDto
                    {
                        ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                        Url = new Uri("https://example.com/file.pdf")
                    }
                ]
            };

            modify?.Invoke(attachment);
            transmission.Attachments.Add(attachment);

            return transmission;
        }
    }
}
