using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.BumpFormSaved;

public sealed class BumpFormSavedCommand : IRequest<BumpFormSavedResult>
{
    public Guid DialogId { get; set; }

    public DateTimeOffset? FormSavedAt { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }
}

public sealed record BumpFormSavedSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class BumpFormSavedResult : OneOfBase<BumpFormSavedSuccess, Forbidden, DomainError, EntityNotFound, ConcurrencyError>;

internal sealed class BumpFormSavedCommandHandler(IDialogDbContext db, IUserResourceRegistry userResourceRegistry, IUnitOfWork unitOfWork) : IRequestHandler<BumpFormSavedCommand, BumpFormSavedResult>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUserResourceRegistry _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<BumpFormSavedResult> Handle(BumpFormSavedCommand request, CancellationToken cancellationToken)
    {
        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("Requires admin scope");
        }
        var fromSavedAt = request.FormSavedAt ?? DateTimeOffset.UtcNow;

        var dialog = await _db.Dialogs
            .Include(x => x.Activities)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Status.Id != DialogStatus.Values.Draft)
        {
            return new DomainError(new DomainFailure(nameof(DialogEntity.Status), "Can only bump when status is Draft"));
        }

        if (dialog.UpdatedAt < fromSavedAt)
        {
            return new DomainError(new DomainFailure(nameof(DialogEntity.UpdatedAt), "Cannot bump to older timestamp"));
        }

        var activity = dialog.Activities.FirstOrDefault();

        if (activity is not { TypeId: DialogActivityType.Values.FormSaved })
        {
            return new DomainError(new DomainFailure(nameof(DialogEntity.Activities), "Latest activity is not of type FormSaved"));
        }


        dialog.Activities.First().CreatedAt = fromSavedAt;
        dialog.UpdatedAt = fromSavedAt;

        var result = await _unitOfWork
            .DisableImmutableFilter()
            .DisableUpdatableFilter()
            .DisableAggregateFilter()
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision).SaveChangesAsync(cancellationToken);

        return result.Match<BumpFormSavedResult>(
            _ => new BumpFormSavedSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError
        );
    }

}
