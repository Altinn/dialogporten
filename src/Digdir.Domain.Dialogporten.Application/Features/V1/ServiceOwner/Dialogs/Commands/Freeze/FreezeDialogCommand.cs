using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogCommand : IRequest<FreezeDialogResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public Guid Id { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }
}

[GenerateOneOf]
public sealed partial class FreezeDialogResult : OneOfBase<FreezeDialogSuccess, EntityNotFound, EntityDeleted, Forbidden, ConcurrencyError>;

public sealed record FreezeDialogSuccess(Guid Revision);

internal sealed class FreezeDialogCommandHandler(
    IDialogDbContext db,
    IUnitOfWork unitOfWork,
    IUserResourceRegistry userResourceRegistry
) : IRequestHandler<FreezeDialogCommand, FreezeDialogResult>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IUserResourceRegistry _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));

    public async Task<FreezeDialogResult> Handle(FreezeDialogCommand request, CancellationToken cancellationToken)
    {
        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("Only ServiceOwner admin can freeze dialogs.");
        }
        var dialog = await _db.Dialogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.Id);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.Id);
        }

        if (dialog.Frozen)
        {
            return new FreezeDialogSuccess(dialog.Revision);
        }

        dialog.Frozen = true;

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<FreezeDialogResult>(
            success => new FreezeDialogSuccess(dialog.Revision),
            domainError => throw new UnreachableException("Should never get a domain error when freezing a dialog"),
            concurrencyError => concurrencyError);
    }
}
