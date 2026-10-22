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

public sealed class PurgeDialogCommand : IRequest<PurgeDialogResult>,
    ISilentUpdater,
    IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
    public bool IsSilentUpdate { get; set; }
}

[GenerateOneOf]
public sealed partial class PurgeDialogResult : OneOfBase<Success, EntityNotFound, Forbidden, ConcurrencyError, ValidationError, Conflict>;

internal sealed class PurgeDialogCommandHandler : IRequestHandler<PurgeDialogCommand, PurgeDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IDialogRelationRepository _relationRepository;

    public PurgeDialogCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IUserResourceRegistry userResourceRegistry,
        IDialogRelationRepository relationRepository)
    {
        _relationRepository = relationRepository ?? throw new ArgumentNullException(nameof(relationRepository));
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(userResourceRegistry);

        _db = db;
        _unitOfWork = unitOfWork;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<PurgeDialogResult> Handle(PurgeDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db.Dialogs
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

        if (request.IfMatchDialogRevision is { } revision && revision != dialog.Revision)
        {
            return new ConcurrencyError();
        }

        if (dialog.HasRelations)
        {
            // The relation rows cascade with the dialog. The related dialogs are not bumped, but those losing
            // their last relation get HasRelations cleared, in the same transaction.
            await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            var relatedDialogIds = (await _relationRepository.GetRelations(dialog.Id, type: null, after: null, int.MaxValue, cancellationToken))
                .Select(x => x.RelatedDialogId)
                .ToList();
            await _relationRepository.LockDialogs([dialog.Id, .. relatedDialogIds], cancellationToken);
            await _relationRepository.ClearHasRelationsWhereNoneLeft(relatedDialogIds, ignoredDialogId: dialog.Id, cancellationToken);
        }

        _db.Dialogs.HardRemove(dialog);
        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<PurgeDialogResult>(
            success => success,
            domainError => throw new UnreachableException("Should never get a domain error when deleting a dialog"),
            concurrencyError => concurrencyError,
            conflict => conflict);
    }
}
