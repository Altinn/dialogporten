using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Actors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;

public sealed class BulkSetSystemLabelCommand : IRequest<BulkSetSystemLabelResult>
{
    public IReadOnlyCollection<Guid> DialogIds { get; init; } = Array.Empty<Guid>();
    public string EnduserId { get; init; } = null!;
    public IReadOnlyCollection<SystemLabel.Values> SystemLabels { get; init; } = Array.Empty<SystemLabel.Values>();
    public Guid? IfMatchEnduserContextRevision { get; init; }
}

public sealed record BulkSetSystemLabelSuccess;

[GenerateOneOf]
public sealed partial class BulkSetSystemLabelResult : OneOfBase<BulkSetSystemLabelSuccess, Forbidden, DomainError, ValidationError, ConcurrencyError>;

internal sealed class BulkSetSystemLabelCommandHandler : IRequestHandler<BulkSetSystemLabelCommand, BulkSetSystemLabelResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public BulkSetSystemLabelCommandHandler(
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

    public async Task<BulkSetSystemLabelResult> Handle(BulkSetSystemLabelCommand request, CancellationToken cancellationToken)
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
        var newLabel = request.SystemLabels.Count switch // The domain model currently only supports one system label
        {
            0 => SystemLabel.Values.Default,
            1 => request.SystemLabels.First(),
            _ => throw new UnreachableException() // Should be caught in validator
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
        return saveResult.Match<BulkSetSystemLabelResult>(
            _ => new BulkSetSystemLabelSuccess(),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
