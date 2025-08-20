using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class UpdateDialogDataLoader(FullDialogAggregateDataLoader fullDialogDataLoader) : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, DialogEntity, UpdateDialogDataLoader>
{
    public override Task<DialogEntity?> Load(UpdateDialogCommand request, CancellationToken cancellationToken) =>
        fullDialogDataLoader.LoadDialogEntity(request.Id, cancellationToken);
}
