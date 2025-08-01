using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal sealed class GetDialogDataLoader : TypedDataLoader<GetDialogQuery, GetDialogResult, DialogEntity, GetDialogDataLoader>
{
    private readonly FullDialogAggregateDataLoader _fullDialogAggregateDataLoader;

    public GetDialogDataLoader(FullDialogAggregateDataLoader fullDialogAggregateDataLoader)
    {
        _fullDialogAggregateDataLoader = fullDialogAggregateDataLoader;
    }
    public override async Task<DialogEntity?> Load(GetDialogQuery request, CancellationToken cancellationToken)
    {
        return await _fullDialogAggregateDataLoader.LoadDialogEntity(request.DialogId, cancellationToken);
    }
}
