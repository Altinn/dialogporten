using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class UpdateDialogDataLoader : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, DialogEntity, UpdateDialogDataLoader>
{
    private readonly FullDialogAggregateDataLoader _fullDialogDataLoader;
    public UpdateDialogDataLoader(FullDialogAggregateDataLoader fullDialogDataLoader)
    {
        _fullDialogDataLoader = fullDialogDataLoader;
    }

    public override async Task<DialogEntity?> Load(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        return await _fullDialogDataLoader.LoadDialogEntity(request.Id, cancellationToken);
    }
}
