using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Relations;

/// <summary>
/// Loads dialogs for the relation endpoints, scoped to the dialogs the current service owner owns (by org).
/// </summary>
internal interface IServiceOwnerDialogLoader
{
    bool IsAdmin { get; }

    /// <summary>Loads (and tracks) the dialog if the caller owns it, including soft-deleted dialogs.</summary>
    Task<DialogEntity?> LoadOwnedDialog(Guid dialogId, CancellationToken cancellationToken);

    /// <summary>Loads (and tracks) a non-deleted dialog, whoever owns it.</summary>
    Task<DialogEntity?> LoadDialog(Guid dialogId, CancellationToken cancellationToken);

    /// <summary>Whether the caller owns <paramref name="dialog"/> or the dialog <paramref name="otherDialogId"/>.</summary>
    Task<bool> OwnsEither(DialogEntity dialog, Guid otherDialogId, CancellationToken cancellationToken);
}

internal sealed class ServiceOwnerDialogLoader : IServiceOwnerDialogLoader
{
    private readonly IDialogDbContext _db;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public ServiceOwnerDialogLoader(IDialogDbContext db, IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public bool IsAdmin => _userResourceRegistry.IsCurrentUserServiceOwnerAdmin();

    public async Task<DialogEntity?> LoadOwnedDialog(Guid dialogId, CancellationToken cancellationToken)
    {
        var org = IsAdmin ? null : await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
        return await _db.Dialogs
            .IgnoreQueryFilters()
            .WhereIf(org is not null, x => x.Org == org)
            .FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
    }

    public Task<DialogEntity?> LoadDialog(Guid dialogId, CancellationToken cancellationToken) =>
        _db.Dialogs.FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);

    public async Task<bool> OwnsEither(DialogEntity dialog, Guid otherDialogId, CancellationToken cancellationToken)
    {
        if (IsAdmin)
        {
            return true;
        }

        var org = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
        return dialog.Org == org || await _db.Dialogs
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == otherDialogId && x.Org == org, cancellationToken);
    }
}
