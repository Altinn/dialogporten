using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogCommand : IRequest<FreezeDialogResult>
{
    public Guid Id { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }
}

[GenerateOneOf]
public sealed partial class FreezeDialogResult : OneOfBase<Success, EntityNotFound, Forbidden, ConcurrencyError>;

internal sealed class FreezeDialogCommandHandler(
    IDialogDbContext db,
    IUnitOfWork unitOfWork,
    IUserResourceRegistry userResourceRegistry,
    IUser user
) : IRequestHandler<FreezeDialogCommand, FreezeDialogResult>
{
    [SuppressMessage("Style", "IDE0052:Remove unread private members")]
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IUserResourceRegistry _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));

    public async Task<FreezeDialogResult> Handle(FreezeDialogCommand request, CancellationToken cancellationToken)
    {

        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            // Amund: husk
            return new Forbidden($".");
        }
        var dialog = await _db.Dialogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.Id);
        }

        // Amund: Skal du kunne freeze slettet dialoger?
        // if (dialog.Deleted)
        // {
        //     return new EntityDeleted<DialogEntity>(request.Id);
        // }

        if (dialog.Frozen)
        {
            return new Success();
        }

        dialog.Frozen = true;

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<FreezeDialogResult>(
            success => new Success(),
            domainError => throw new UnreachableException("Should never get a domain error when freezing a dialog"),
            concurrencyError => concurrencyError);
    }
}
