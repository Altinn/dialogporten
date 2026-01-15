using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetSystemLabelCommand : IRequest<BulkSetSystemLabelResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public string? EndUserId { get; set; } // See ServiceOwnerOnBehalfOfPersonMiddleware
    public BulkSetSystemLabelDto Dto { get; set; } = new();
}

public sealed record BulkSetSystemLabelSuccess;

[GenerateOneOf]
public sealed partial class BulkSetSystemLabelResult : OneOfBase<BulkSetSystemLabelSuccess, Forbidden, DomainError, ValidationError, ConcurrencyError, Conflict>;

internal sealed class BulkSetSystemLabelCommandHandler : IRequestHandler<BulkSetSystemLabelCommand, BulkSetSystemLabelResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public BulkSetSystemLabelCommandHandler(
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

    public async Task<BulkSetSystemLabelResult> Handle(BulkSetSystemLabelCommand request, CancellationToken cancellationToken)
    {
        var (performedBy, forbidden) = await TryCreatePerformedByActor(request, cancellationToken);
        if (forbidden is not null)
        {
            return forbidden;
        }

        List<DialogEntity> dialogs;

        if (_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            dialogs = await _db.Dialogs
                .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
                .Where(x => request.Dto.Dialogs.Select(d => d.DialogId).Contains(x.Id))
                .ToListAsync(cancellationToken);
        }
        else
        {
            var authorizedResources =
                await _altinnAuthorization.GetAuthorizedResourcesForSearch([], [], cancellationToken);

            dialogs = await _db.Dialogs
                .PrefilterAuthorizedDialogs(authorizedResources)
                .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
                .Where(x => request.Dto.Dialogs.Select(d => d.DialogId).Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        if (dialogs.Count != request.Dto.Dialogs.Count)
        {
            var found = dialogs.Select(x => x.Id).ToHashSet();
            var missing = request.Dto.Dialogs.Select(d => d.DialogId).Where(id => !found.Contains(id)).ToList();
            return new Forbidden().WithInvalidDialogIds(missing);
        }


        foreach (var dialog in dialogs)
        {
            dialog.EndUserContext.UpdateSystemLabels(
                request.Dto.AddLabels,
                request.Dto.RemoveLabels,
                performedBy!);

            _unitOfWork.EnableConcurrencyCheck(dialog.EndUserContext, request.Dto.Dialogs.Single(x => x.DialogId == dialog.Id).EndUserContextRevision);
        }

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<BulkSetSystemLabelResult>(
            _ => new BulkSetSystemLabelSuccess(),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }

    private async Task<(LabelAssignmentLogActor? Actor, Forbidden? Error)> TryCreatePerformedByActor(
        BulkSetSystemLabelCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Dto.PerformedBy is not null)
        {
            if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
            {
                return (null, new Forbidden("performedBy is only allowed for admin-integrations."));
            }

            var actor = LabelAssignmentLogActorFactory.Create(
                request.Dto.PerformedBy.ActorType,
                request.Dto.PerformedBy.ActorId,
                request.Dto.PerformedBy.ActorName);
            return (actor, null);
        }

        var userInfo = await _userRegistry.GetCurrentUserInformation(cancellationToken);
        return (LabelAssignmentLogActorFactory.FromUserInformation(userInfo), null);
    }
}
