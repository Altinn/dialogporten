using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class UpdateDialogDataLoader : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, DialogEntity, UpdateDialogDataLoader>
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public UpdateDialogDataLoader(IDialogDbContext dialogDbContext, IUserResourceRegistry userResourceRegistry)
    {
        _dialogDbContext = dialogDbContext;
        _userResourceRegistry = userResourceRegistry;
    }

    public override async Task<DialogEntity?> Load(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        return await _dialogDbContext.Dialogs
            .Include(x => x.Activities)
            .Include(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .Include(x => x.SearchTags)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.DisplayName!.Localizations)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.Urls)
            .Include(x => x.GuiActions)
                .ThenInclude(x => x.Title!.Localizations)
            .Include(x => x.GuiActions)
                .ThenInclude(x => x.Prompt!.Localizations)
            .Include(x => x.ApiActions)
                .ThenInclude(x => x.Endpoints)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Content)
            .Include(x => x.DialogEndUserContext)
            .IgnoreQueryFilters()
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
    }
}
