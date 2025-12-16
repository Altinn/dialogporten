using System.Linq;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

internal sealed class CreateTransmissionDataLoader : TypedDataLoader<CreateTransmissionCommand, CreateTransmissionResult, DialogEntity, CreateTransmissionDataLoader>
{
    private readonly IDialogDbContext _dbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public CreateTransmissionDataLoader(IDialogDbContext dbContext, IUserResourceRegistry userResourceRegistry)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public override async Task<DialogEntity?> Load(CreateTransmissionCommand request, CancellationToken cancellationToken)
    {
        var isAdmin = _userResourceRegistry.IsCurrentUserServiceOwnerAdmin();
        var org = string.Empty;
        if (!isAdmin)
        {
            org = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
        }

        return await _dbContext.Dialogs
            .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .IgnoreQueryFilters()
            .WhereIf(!isAdmin,
                x => x.Org == org)
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);
    }
}
