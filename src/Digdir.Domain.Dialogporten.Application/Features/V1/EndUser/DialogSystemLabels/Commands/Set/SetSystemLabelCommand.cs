using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.Set;

public sealed class SetSystemLabelCommand : IRequest<SetSystemLabelResult>
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchEnduserContextRevision { get; set; }
    public SystemLabel.Values Label { get; set; }
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

    public SetSystemLabelCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork, IUserRegistry userRegistry, IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<SetSystemLabelResult> Handle(
        SetSystemLabelCommand request,
        CancellationToken cancellationToken)
    {
        var dialog = await _db.Dialogs
                              .Include(x => x.DialogEndUserContext)
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

        var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);

        dialog.DialogEndUserContext.UpdateLabel(request.Label, currentUserInformation.UserId.ExternalIdWithPrefix);

        var saveResult = await _unitOfWork
                               .EnableConcurrencyCheck(dialog.DialogEndUserContext, request.IfMatchEnduserContextRevision)
                               .SaveChangesAsync(cancellationToken);

        return saveResult.Match<SetSystemLabelResult>(
            success => new SetSystemLabelSuccess(dialog.DialogEndUserContext.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
