using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

// TODO: The hierarchy validator can now fetch only the referenced transmissions,
// so the preloader no longer needs to hydrate the full transmission graph to validate updates.
internal sealed class UpdateDialogDataLoader(FullDialogAggregateDataLoader fullDialogDataLoader) : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, DialogEntity, UpdateDialogDataLoader>
{
    public override Task<DialogEntity?> Load(UpdateDialogCommand request, CancellationToken cancellationToken) =>
        fullDialogDataLoader.LoadDialogEntity(request.Id, cancellationToken);
}
