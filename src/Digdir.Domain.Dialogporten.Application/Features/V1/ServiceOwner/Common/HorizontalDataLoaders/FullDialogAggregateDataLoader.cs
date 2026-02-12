using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;

public sealed class FullDialogAggregateDataLoader
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly Dictionary<Guid, DialogEntity> _dialogEntities = new();

    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogSearchTag>>> OrderedSearchTags =
        e => e.SearchTags.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogAttachment>>> OrderedAttachments =
        e => e.Attachments.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogGuiAction>>> OrderedGuiActions =
        e => e.GuiActions.OrderBy(g => g.CreatedAt).ThenBy(g => g.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogApiAction>>> OrderedApiActions =
        e => e.ApiActions.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogTransmission>>> OrderedTransmissions =
        e => e.Transmissions.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogActivity>>> OrderedActivities =
        e => e.Activities.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogSeenLog>>> OrderedSeenLog =
        e => e.SeenLog.OrderBy(s => s.CreatedAt);
    private static readonly Expression<Func<DialogTransmission, IEnumerable<DialogTransmissionContent>>> OrderedTransmissionContent =
        t => t.Content.OrderBy(c => c.Id).ThenBy(c => c.CreatedAt);
    private static readonly Expression<Func<DialogTransmission, IEnumerable<DialogTransmissionAttachment>>> OrderedTransmissionAttachments =
        t => t.Attachments.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);
    private static readonly Expression<Func<DialogTransmission, IEnumerable<DialogTransmissionNavigationalAction>>> OrderedTransmissionNavigationalActions =
        t => t.NavigationalActions.OrderBy(n => n.CreatedAt).ThenBy(n => n.Id);
    private static readonly Expression<Func<DialogEndUserContext, IEnumerable<DialogEndUserContextSystemLabel>>> OrderedEndUserContextSystemLabels =
        c => c.DialogEndUserContextSystemLabels.OrderBy(l => l.CreatedAt).ThenBy(l => l.SystemLabelId);
    private static readonly Expression<Func<DialogServiceOwnerContext, IEnumerable<DialogServiceOwnerLabel>>> OrderedServiceOwnerLabels =
        c => c.ServiceOwnerLabels.OrderBy(l => l.Value);
    private static readonly Expression<Func<DialogEntity, IEnumerable<DialogContent>>> OrderedContent =
        e => e.Content.OrderBy(c => c.Id).ThenBy(c => c.CreatedAt);

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

        var dialogEntity = await _dialogDbContext.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .Include(OrderedSearchTags)
                .Include(OrderedContent).ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedAttachments).ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedAttachments).ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .Include(OrderedGuiActions).ThenInclude(x => x.Title!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedGuiActions).ThenInclude(x => x!.Prompt!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedApiActions).ThenInclude(x => x.Endpoints.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .Include(OrderedTransmissions).ThenInclude(OrderedTransmissionContent).ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedTransmissions).ThenInclude(x => x.Sender).ThenInclude(x => x.ActorNameEntity)
                .Include(OrderedTransmissions).ThenInclude(OrderedTransmissionAttachments).ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .Include(OrderedTransmissions).ThenInclude(OrderedTransmissionAttachments).ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedTransmissions).ThenInclude(OrderedTransmissionNavigationalActions).ThenInclude(x => x.Title.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedActivities).ThenInclude(x => x.Description!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(OrderedActivities).ThenInclude(x => x.PerformedBy).ThenInclude(x => x.ActorNameEntity)
                .Include(OrderedSeenLog).ThenInclude(x => x.SeenBy).ThenInclude(x => x.ActorNameEntity)
                .Include(x => x.EndUserContext).ThenInclude(OrderedEndUserContextSystemLabels)
                .Include(x => x.ServiceOwnerContext).ThenInclude(OrderedServiceOwnerLabels)
                .IgnoreQueryFilters()
                .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(),
                    x => resourceIds.Contains(x.ServiceResource))
                .FirstOrDefaultAsync(x => x.Id == dialogId, ct),
            cancellationToken);

        if (dialogEntity is not null)
        {
            _dialogEntities.TryAdd(dialogId, dialogEntity);
            return dialogEntity;
        }

        return null;
    }
}
