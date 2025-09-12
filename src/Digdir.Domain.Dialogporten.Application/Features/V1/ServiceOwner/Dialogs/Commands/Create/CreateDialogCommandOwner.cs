using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal sealed class CreateDialogCommandResolver(IResourceRegistry resourceRegistry) : IServiceResourceResolver<CreateDialogCommand>
{
    public async Task<(string? ServiceResource, string? OwnerOrg)> Resolve(CreateDialogCommand command, CancellationToken cancellationToken)
    {
        var serviceResourceInformation = await resourceRegistry.GetResourceInformation(command.Dto.ServiceResource, cancellationToken);
        return (command.Dto.ServiceResource, serviceResourceInformation?.OwnOrgShortName);
    }
}
