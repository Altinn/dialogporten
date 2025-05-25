using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

internal sealed class UpdateServiceOwnerContextDataLoader : TypedDataLoader<UpdateDialogServiceOwnerContextCommand,
    UpdateDialogServiceOwnerContextResult, DialogServiceOwnerContext,
    UpdateServiceOwnerContextDataLoader>
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public UpdateServiceOwnerContextDataLoader(IDialogDbContext dialogDbContext, IUserResourceRegistry userResourceRegistry)
    {
        _dialogDbContext = dialogDbContext ?? throw new ArgumentNullException(nameof(dialogDbContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public override async Task<DialogServiceOwnerContext?> Load(
        UpdateDialogServiceOwnerContextCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var serviceOwnerContext = await _dialogDbContext
            .DialogServiceOwnerContexts
            .Include(x => x.ServiceOwnerLabels)
            .Where(x => x.Dialog.Id == request.DialogId)
            .Where(x => resourceIds.Contains(x.Dialog.ServiceResource))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return serviceOwnerContext;
    }
}
