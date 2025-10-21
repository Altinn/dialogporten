using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;

public sealed class FullDialogAggregateDataLoader
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly Dictionary<Guid, DialogEntity> _dialogEntities = new();

    public FullDialogAggregateDataLoader(IDialogDbContext dialogDbContext, IUserResourceRegistry userResourceRegistry)
    {
        _dialogDbContext = dialogDbContext;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<DialogEntity?> LoadDialogEntity(Guid dialogId, CancellationToken cancellationToken)
    {
        if (_dialogEntities.TryGetValue(dialogId, out var entity))
        {
            return entity;
        }

        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        DialogEntity? ret;
        // With Postgre Snapshot gets mapped to ReadCommited Amund: Sauce?
        await using var dbTransaction = await _dialogDbContext.BeginTransactionAsync(cancellationToken);
        var dialogEntity = await _dialogDbContext.Dialogs
            .Include(x => x.Content.OrderBy(x => x.Id).ThenBy(x => x.CreatedAt))
                .ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.SearchTags.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Title!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x!.Prompt!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.ApiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Endpoints.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Sender)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                .ThenInclude(x => x.Urls)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                .ThenInclude(x => x.DisplayName!.Localizations)
            .Include(x => x.Activities)
                .ThenInclude(x => x.Description!.Localizations)
            .Include(x => x.Activities)
                .ThenInclude(x => x.PerformedBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.SeenLog
                .OrderBy(x => x.CreatedAt))
                .ThenInclude(x => x.SeenBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.EndUserContext.DialogEndUserContextSystemLabels)
            .Include(x => x.ServiceOwnerContext)
                .ThenInclude(x => x.ServiceOwnerLabels.OrderBy(x => x.Value))
            .IgnoreQueryFilters()
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);

        // Commit the transaction
        await dbTransaction.CommitAsync(cancellationToken);

        if (dialogEntity is not null)
        {
            _dialogEntities.TryAdd(dialogId, dialogEntity);
            ret = dialogEntity;
        }
        else
        {
            ret = null;
        }

        return ret;

    }
}
