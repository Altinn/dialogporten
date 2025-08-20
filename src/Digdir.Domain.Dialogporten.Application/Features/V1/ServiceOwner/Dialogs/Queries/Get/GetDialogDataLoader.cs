using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal sealed class GetDialogDataLoader(FullDialogAggregateDataLoader fullDialogAggregateDataLoader)
    : TypedDataLoader<GetDialogQuery, GetDialogResult, DialogEntity, GetDialogDataLoader>
{
    public override Task<DialogEntity?> Load(GetDialogQuery request, CancellationToken cancellationToken) =>
        fullDialogAggregateDataLoader.LoadDialogEntity(request.DialogId, cancellationToken);
}
