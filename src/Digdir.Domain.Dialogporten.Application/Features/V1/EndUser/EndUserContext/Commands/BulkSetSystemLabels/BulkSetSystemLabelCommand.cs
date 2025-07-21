using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetSystemLabelCommand : IRequest<BulkSetSystemLabelResult>
{
    public BulkSetSystemLabelDto Dto { get; set; } = new();
}

public sealed record BulkSetSystemLabelSuccess;

[GenerateOneOf]
public sealed partial class BulkSetSystemLabelResult : OneOfBase<BulkSetSystemLabelSuccess, EntityNotFound, DomainError, ValidationError, ConcurrencyError>;

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
        var dialogIds = request.Dto.Dialogs
            .Select(d => d.DialogId)
            .ToList();

        var (distinctParties, distinctServiceResources) =
            await GetDistinctPartiesAndServiceResources(dialogIds, cancellationToken);

        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            distinctParties, distinctServiceResources, cancellationToken);

        var dialogs = await _db.Dialogs
            .PrefilterAuthorizedDialogs(authorizedResources)
            .Include(x => x.EndUserContext)
            .Where(x => dialogIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var notFound = request.Dto.Dialogs
            .Select(x => x.DialogId)
            .Except(dialogs.Select(d => d.Id))
            .ToList();

        if (notFound.Count > 0)
        {
            return new EntityNotFound<DialogEntity>(notFound);
        }

        // The domain model currently only supports one system label
        var newLabel = request.Dto.SystemLabels.SingleOrDefault(SystemLabel.Values.Default);

        await dialogs.MergeAsync(
            sources: request.Dto.Dialogs,
            destinationKeySelector: x => x.Id,
            sourceKeySelector: x => x.DialogId,
            update: (sets, ct) => UpdateDialogs(sets, newLabel, ct),
            cancellationToken: cancellationToken);

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<BulkSetSystemLabelResult>(
            _ => new BulkSetSystemLabelSuccess(),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }

    private async Task<(List<string>, List<string>)> GetDistinctPartiesAndServiceResources(
        List<Guid> dialogIds,
        CancellationToken cancellationToken)
    {
        var relevantServiceResources = await _db.Dialogs
            .Where(x => dialogIds.Contains(x.Id))
            .Select(x => new { x.ServiceResource, x.Party })
            .ToListAsync(cancellationToken);
        var distinctParties = relevantServiceResources
            .Select(x => x.Party)
            .Distinct()
            .ToList();
        var distinctServiceResources = relevantServiceResources
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToList();
        return (distinctParties, distinctServiceResources);
    }

    private async Task UpdateDialogs(
        IEnumerable<UpdateSet<DialogEntity, DialogRevisionDto>> updateSets,
        SystemLabel.Values newLabel,
        CancellationToken cancellationToken)
    {
        var userInfo = await _userRegistry.GetCurrentUserInformation(cancellationToken);
        foreach (var (dto, entity) in updateSets)
        {
            entity.UpdateSystemLabel(userInfo.UserId.ExternalIdWithPrefix, newLabel);
            _unitOfWork.EnableConcurrencyCheck(entity.EndUserContext, dto.EndUserContextRevision);
        }
    }
}
