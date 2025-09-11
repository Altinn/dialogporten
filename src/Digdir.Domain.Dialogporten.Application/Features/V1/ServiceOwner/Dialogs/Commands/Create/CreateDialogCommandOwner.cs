using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal sealed class CreateDialogCommandOwner(IResourceRegistry resourceRegistry) : IRequestOwner<CreateDialogCommand>
{
    public async Task<(string? ServiceResource, string? OwnerOrg)> GetOwnerInformation(CreateDialogCommand command, CancellationToken cancellationToken)
    {
        var serviceResourceInformation = await resourceRegistry.GetResourceInformation(command.Dto.ServiceResource, cancellationToken);
        return (command.Dto.ServiceResource, serviceResourceInformation?.OwnOrgShortName);
    }
}
