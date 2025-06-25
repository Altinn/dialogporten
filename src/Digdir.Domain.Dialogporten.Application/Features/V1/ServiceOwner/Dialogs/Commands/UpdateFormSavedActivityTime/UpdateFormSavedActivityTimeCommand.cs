using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;

public sealed class UpdateFormSavedActivityTimeCommand : IRequest<BumpFormSavedResult>, ISilentUpdater
{
    public Guid DialogId { get; set; }

    public Guid ActivityId { get; set; }

    public DateTimeOffset NewCreatedAt { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }
}

public sealed record BumpFormSavedSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class BumpFormSavedResult : OneOfBase<BumpFormSavedSuccess, Forbidden, DomainError, EntityNotFound, ConcurrencyError>;

internal sealed class BumpFormSavedCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork, IUserResourceRegistry userResourceRegistry)
    : IRequestHandler<UpdateFormSavedActivityTimeCommand, BumpFormSavedResult>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IUserResourceRegistry _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));

    public async Task<BumpFormSavedResult> Handle(UpdateFormSavedActivityTimeCommand request, CancellationToken cancellationToken)
    {
        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("Requires admin scope");
        }

        var activity = await _db.DialogActivities
            .Include(x => x.Dialog)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ActivityId && x.DialogId == request.DialogId, cancellationToken);

        if (activity is null)
        {
            return new EntityNotFound<DialogActivity>(request.ActivityId);
        }

        if (activity.TypeId is not DialogActivityType.Values.FormSaved)
        {
            return new DomainError(new DomainFailure(nameof(DialogActivity.Type), $"Only {nameof(DialogActivityType.Values.FormSaved)} activities is allowed to be updated using admin scope."));
        }

        activity.CreatedAt = request.NewCreatedAt;

        // Since we are opting out of UpdatableFilter through ISilentUpdater
        // we need to manually update Dialog.UpdatedAt conditionally, and
        // always Dialog.Revision.
        activity.Dialog.UpdatedAt = activity.Dialog.UpdatedAt < activity.CreatedAt
            ? activity.CreatedAt
            : activity.Dialog.UpdatedAt;
        activity.Dialog.NewVersion();

        var result = await _unitOfWork
            .DisableImmutableFilter()
            .EnableConcurrencyCheck(activity.Dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return result.Match<BumpFormSavedResult>(
            _ => new BumpFormSavedSuccess(activity.Dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError
        );
    }
}
