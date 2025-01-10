using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.SoftDeletable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;

public sealed class RestoreDialogCommand : IRequest<RestoreDialogResult>
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
}

[GenerateOneOf]
public sealed partial class RestoreDialogResult : OneOfBase<RestoreDialogSuccess, EntityNotFound, Forbidden, ConcurrencyError>;

public sealed record RestoreDialogSuccess(Guid Revision);

internal sealed class RestoreDialogCommandHandler : IRequestHandler<RestoreDialogCommand, RestoreDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public RestoreDialogCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork, IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<RestoreDialogResult> Handle(RestoreDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db.Dialogs
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        // Amund: More Checks
        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (!dialog.Deleted)
        {
            return new RestoreDialogSuccess(dialog.Revision);
        }

        dialog.Restore(); // Amund: Restore lager ny Rev

        // Amund Q: ConcurrencyCheck weird, Manuelt laging av ny rev vil feile checken?. Dual writes == Bad, må gjøres i en write
        var saveResult = await _unitOfWork
            .DisableUpdatableFilter()
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<RestoreDialogResult>(
            success => new RestoreDialogSuccess(dialog.Revision),
            domainError => throw new UnreachableException("Should never get a domain error when restoring a dialog"),
            concurrencyError => concurrencyError
        );
    }
}
