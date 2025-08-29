using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;

public sealed class SetSystemLabelCommand : IRequest<SetSystemLabelResult>
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchEndUserContextRevision { get; set; }
    public IReadOnlyCollection<SystemLabel.Values> AddLabels { get; set; } = [];
    public IReadOnlyCollection<SystemLabel.Values> RemoveLabels { get; set; } = [];
}

public sealed record SetSystemLabelSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class SetSystemLabelResult : OneOfBase<SetSystemLabelSuccess, EntityNotFound, EntityDeleted, DomainError, ValidationError, ConcurrencyError>;

internal sealed class SetSystemLabelCommandHandler : IRequestHandler<SetSystemLabelCommand, SetSystemLabelResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IApplicationContext _applicationContext;

    public SetSystemLabelCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork, IUserRegistry userRegistry, IAltinnAuthorization altinnAuthorization, IApplicationContext applicationContext)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
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

        var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(dialog, cancellationToken: cancellationToken);
        if (!authorizationResult.HasAccessToMainResource())
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        // Add metadata for cost management after authorization
        _applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceOrg, dialog.Org);
        _applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceResource, dialog.ServiceResource);

        var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);

        dialog.EndUserContext.UpdateSystemLabels(request.AddLabels, request.RemoveLabels, currentUserInformation.UserId.ExternalIdWithPrefix);

        var saveResult = await _unitOfWork
                               .EnableConcurrencyCheck(dialog.EndUserContext, request.IfMatchEndUserContextRevision)
                               .SaveChangesAsync(cancellationToken);

        return saveResult.Match<SetSystemLabelResult>(
            _ => new SetSystemLabelSuccess(dialog.EndUserContext.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
