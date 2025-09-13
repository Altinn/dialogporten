using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.EntityFrameworkCore.Features.SoftDeletable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
public sealed class PurgeDialogCommand : IRequest<PurgeDialogResult>, ISilentUpdater, IDialogIdQuery
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
    public bool IsSilentUpdate { get; set; }
}

[GenerateOneOf]
public sealed partial class PurgeDialogResult : OneOfBase<Success, EntityNotFound, Forbidden, ConcurrencyError, ValidationError>;

internal sealed class PurgeDialogCommandHandler : IRequestHandler<PurgeDialogCommand, PurgeDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public PurgeDialogCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<PurgeDialogResult> Handle(PurgeDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db.Dialogs
            .Include(x => x.Attachments)
            .Include(x => x.Activities)
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }


        if (!_userResourceRegistry.UserCanModifyResourceType(dialog.ServiceResourceType))
        {
            return new Forbidden($"User cannot modify resource type {dialog.ServiceResourceType}.");
        }
        _db.Dialogs.HardRemove(dialog);
        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<PurgeDialogResult>(
            success => success,
            domainError => throw new UnreachableException("Should never get a domain error when deleting a dialog"),
            concurrencyError => concurrencyError);
    }
}
