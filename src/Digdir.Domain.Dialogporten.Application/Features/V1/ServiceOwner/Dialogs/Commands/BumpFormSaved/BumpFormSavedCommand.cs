using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.BumpFormSaved;

public sealed class BumpFormSavedCommand : IRequest<BumpFormSavedResult>, ISilentUpdater
{
    public Guid DialogId { get; set; }

    public DateTimeOffset? FormSavedAt { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }

    public bool IsSilentUpdate => true;
}

public sealed record BumpFormSavedSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class BumpFormSavedResult : OneOfBase<BumpFormSavedSuccess, Forbidden, DomainError, EntityNotFound, ConcurrencyError>;

internal sealed class BumpFormSavedCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork) : IRequestHandler<BumpFormSavedCommand, BumpFormSavedResult>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<BumpFormSavedResult> Handle(BumpFormSavedCommand request, CancellationToken cancellationToken)
    {
        var formSavedAt = request.FormSavedAt ?? DateTimeOffset.UtcNow;

        var dialog = await _db.Dialogs
            .Include(x => x.Activities)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.StatusId != DialogStatus.Values.Draft)
        {
            return new DomainError(new DomainFailure(nameof(DialogEntity.Status), "Can only bump timestamp when dialog status is Draft"));
        }

        if (dialog.UpdatedAt > formSavedAt)
        {
            return new DomainError(new DomainFailure(nameof(DialogEntity.UpdatedAt), "Cannot bump to timestamp in the past"));
        }

        var activity = dialog.Activities.MaxBy(x => x.CreatedAt);

        if (activity is not { TypeId: DialogActivityType.Values.FormSaved })
        {
            return new DomainError(new DomainFailure(nameof(DialogActivity.Type), "Latest activity is not of type FormSaved"));
        }

        activity.CreatedAt = formSavedAt;
        dialog.UpdatedAt = formSavedAt;

        var result = await _unitOfWork
            .DisableImmutableFilter()
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return result.Match<BumpFormSavedResult>(
            _ => new BumpFormSavedSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError
        );
    }
}
