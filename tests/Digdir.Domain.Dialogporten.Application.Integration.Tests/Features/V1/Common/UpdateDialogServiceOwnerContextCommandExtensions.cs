using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

internal static class UpdateDialogServiceOwnerContextCommandExtensions
{
    public static UpdateDialogServiceOwnerContextCommand AddServiceOwnerLabels(this UpdateDialogServiceOwnerContextCommand command, params string[] labels)
    {
        foreach (var label in labels)
        {
            command.Dto.ServiceOwnerLabels.Add(new ServiceOwnerLabelDto { Value = label });
        }
        return command;
    }
}
