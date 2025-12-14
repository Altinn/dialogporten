using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

/// <summary>
/// Lightweight loader that fetches the persisted transmission counters directly from the dialog row.
/// </summary>
internal sealed class DialogTransmissionCountDataLoader(IDialogDbContext dbContext)
    : TypedDataLoader<UpdateDialogCommand, UpdateDialogResult, int, DialogTransmissionCountDataLoader>
{
    public override async Task<int> Load(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await dbContext.Dialogs
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => x.FromPartyTransmissionsCount + x.FromServiceOwnerTransmissionsCount)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
