using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

// Transmissions are append-only, and every check on them in the update queries the database,
// so the preloader leaves them out. Dialogs can have tens of thousands of them.
internal sealed class UpdateDialogDataLoader(FullDialogAggregateDataLoader fullDialogDataLoader) : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, DialogEntity, UpdateDialogDataLoader>
{
    public override Task<DialogEntity?> Load(UpdateDialogCommand request, CancellationToken cancellationToken) =>
        fullDialogDataLoader.LoadDialogEntityWithoutTransmissions(request.Id, cancellationToken);
}
