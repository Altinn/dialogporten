using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class UpdateDialogExistsDataLoader(
    IDialogDbContext dbContext,
    IUserResourceRegistry userResourceRegistry)
    : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, bool, UpdateDialogExistsDataLoader>
{
    public override async Task<bool> Load(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.Dialogs
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id);

        if (!userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            var org = await userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
            query = query.Where(x => x.Org == org);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
