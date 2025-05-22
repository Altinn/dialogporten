using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkUpdate;

public sealed class BulkUpdateSystemLabelCommand : IRequest<BulkUpdateSystemLabelResult>
{
    public IReadOnlyCollection<Guid> DialogIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<SystemLabel.Values> Labels { get; init; } = Array.Empty<SystemLabel.Values>();
    public Guid? IfMatchEnduserContextRevision { get; init; }
}

public sealed record BulkUpdateSystemLabelSuccess();

[GenerateOneOf]
public sealed partial class BulkUpdateSystemLabelResult : OneOfBase<BulkUpdateSystemLabelSuccess, Forbidden, ValidationError, DomainError, ConcurrencyError>;

internal sealed class BulkUpdateSystemLabelCommandHandler : IRequestHandler<BulkUpdateSystemLabelCommand, BulkUpdateSystemLabelResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public BulkUpdateSystemLabelCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<BulkUpdateSystemLabelResult> Handle(BulkUpdateSystemLabelCommand request, CancellationToken cancellationToken)
    {
        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch([], [], cancellationToken);

        var dialogs = await _db.Dialogs
            .PrefilterAuthorizedDialogs(authorizedResources)
            .Include(x => x.DialogEndUserContext)
            .Where(x => request.DialogIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (dialogs.Count != request.DialogIds.Count)
        {
            var found = dialogs.Select(x => x.Id).ToHashSet();
            var missing = request.DialogIds.Where(id => !found.Contains(id)).ToList();
            return new Forbidden($"The following dialogIds are not valid: {string.Join(",", missing)}");
        }

        var userInfo = await _userRegistry.GetCurrentUserInformation(cancellationToken);
        var newLabel = request.Labels.Count switch
        {
            0 => SystemLabel.Values.Default,
            1 => request.Labels.First(),
            _ => throw new InvalidOperationException("Only one system label is supported")
        };

        foreach (var dialog in dialogs)
        {
            dialog.DialogEndUserContext.UpdateLabel(newLabel, userInfo.UserId.ExternalIdWithPrefix, ActorType.Values.ServiceOwner);
            if (request.IfMatchEnduserContextRevision.HasValue)
            {
                _unitOfWork.EnableConcurrencyCheck(dialog.DialogEndUserContext, request.IfMatchEnduserContextRevision);
            }
        }

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<BulkUpdateSystemLabelResult>(
            _ => new BulkUpdateSystemLabelSuccess(),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
