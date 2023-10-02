﻿using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;

public sealed class DeleteDialogCommand : IRequest<DeleteDialogResult>
{
    public Guid Id { get; set; }
    public Guid? ETag { get; set; }
}

[GenerateOneOf]
public partial class DeleteDialogResult : OneOfBase<Success, EntityNotFound, ConcurrencyError> { }

internal sealed class DeleteDialogCommandHandler : IRequestHandler<DeleteDialogCommand, DeleteDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly UserService _userService;

    public DeleteDialogCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        UserService userService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _userService = userService;
    }

    public async Task<DeleteDialogResult> Handle(DeleteDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userService.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db.Dialogs
            .Where(x => resourceIds.Contains(x.ServiceResource.ToString()))
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.Id);
        }

        _db.TrySetOriginalETag(dialog, request.ETag);

        _db.Dialogs.Remove(dialog);
        _eventPublisher.Publish(
            new DialogDeletedDomainEvent(
                dialog.Id, 
                dialog.ServiceResource.ToString(), 
                dialog.Party));

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<DeleteDialogResult>(
            success => success,
            domainError => throw new UnreachableException("Should never get a domain error when creating a new dialog"),
            concurrencyError => concurrencyError);
    }
}
