using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

public static class UpdateDialogCommandExtensions
{
    public static UpdateDialogCommand AddTransmission(this UpdateDialogCommand command, Action<TransmissionDto>? modify = null)
    {
        var transmission = new TransmissionDto
        {
            Type = DialogTransmissionType.Values.Information,
            Content = new()
            {
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
            },
            Sender = new() { ActorType = ActorType.Values.ServiceOwner }
        };

        modify?.Invoke(transmission);
        command.Dto.Transmissions.Add(transmission);
        return command;
    }
}
