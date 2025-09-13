using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetSystemLabelCommand : IRequest<BulkSetSystemLabelResult>
{
    public string EndUserId { get; set; } = null!; // See ServiceOwnerOnBehalfOfPersonMiddleware
    public BulkSetSystemLabelDto Dto { get; set; } = new();
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
            .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .Where(x => request.Dto.Dialogs.Select(d => d.DialogId).Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (dialogs.Count != request.Dto.Dialogs.Count)
        {
            var found = dialogs.Select(x => x.Id).ToHashSet();
            var missing = request.Dto.Dialogs.Select(d => d.DialogId).Where(id => !found.Contains(id)).ToList();
            return new Forbidden().WithInvalidDialogIds(missing);
        }

        var userInfo = await _userRegistry.GetCurrentUserInformation(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        foreach (var dialog in dialogs)
        {
            dialog.EndUserContext.UpdateSystemLabels(
                request.Dto.AddLabels,
                request.Dto.RemoveLabels,
                userInfo.UserId.ExternalIdWithPrefix);

            _unitOfWork.EnableConcurrencyCheck(dialog.EndUserContext, request.Dto.Dialogs.Single(x => x.DialogId == dialog.Id).EndUserContextRevision);
        }

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<BulkSetSystemLabelResult>(
            _ => new BulkSetSystemLabelSuccess(),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}

internal sealed class BulkSetSystemLabelCommandResolver : IServiceResourceResolver<BulkSetSystemLabelCommand>
{
    public Task<ServiceResourceInformation?> Resolve(BulkSetSystemLabelCommand request, CancellationToken cancellationToken)
    {
        // For bulk operations with mixed entities, we can't attribute to specific org/resource
        return Task.FromResult<ServiceResourceInformation?>(null);
    }
}
