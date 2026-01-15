using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;

public sealed class SetSystemLabelCommand : IRequest<SetSystemLabelResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public string? EndUserId { get; set; } // See ServiceOwnerOnBehalfOfPersonMiddleware
    public ActorDto? PerformedBy { get; set; }
    public Guid? IfMatchEndUserContextRevision { get; set; }

    public IReadOnlyCollection<SystemLabel.Values> AddLabels { get; set; } = [];
    public IReadOnlyCollection<SystemLabel.Values> RemoveLabels { get; set; } = [];
}

public sealed record SetSystemLabelSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class SetSystemLabelResult : OneOfBase<SetSystemLabelSuccess, EntityNotFound, EntityDeleted, Forbidden, DomainError, ValidationError, ConcurrencyError, Conflict>;

internal sealed class SetSystemLabelCommandHandler : IRequestHandler<SetSystemLabelCommand, SetSystemLabelResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SetSystemLabelCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IUserRegistry userRegistry,
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<SetSystemLabelResult> Handle(
        SetSystemLabelCommand request,
        CancellationToken cancellationToken)
    {
        var dialog = await _db.Dialogs
            .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken: cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        var (performedBy, forbidden) = await TryCreatePerformedByActor(request, cancellationToken);
        if (forbidden is not null)
        {
            return forbidden;
        }

        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(dialog, cancellationToken: cancellationToken);
            if (!authorizationResult.HasAccessToMainResource())
            {
                return new EntityNotFound<DialogEntity>(request.DialogId);
            }
        }

        dialog.EndUserContext.UpdateSystemLabels(request.AddLabels, request.RemoveLabels, performedBy!);

        var saveResult = await _unitOfWork
                               .EnableConcurrencyCheck(dialog.EndUserContext, request.IfMatchEndUserContextRevision)
                               .SaveChangesAsync(cancellationToken);

        return saveResult.Match<SetSystemLabelResult>(
            _ => new SetSystemLabelSuccess(dialog.EndUserContext.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }

    private async Task<(LabelAssignmentLogActor? Actor, Forbidden? Error)> TryCreatePerformedByActor(
        SetSystemLabelCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PerformedBy is not null)
        {
            if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
            {
                return (null, new Forbidden("performedBy is only allowed for admin-integrations."));
            }

            var actor = LabelAssignmentLogActorFactory.Create(
                request.PerformedBy.ActorType,
                request.PerformedBy.ActorId,
                request.PerformedBy.ActorName);
            return (actor, null);
        }

        var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);
        return (LabelAssignmentLogActorFactory.FromUserInformation(currentUserInformation), null);
    }
}
