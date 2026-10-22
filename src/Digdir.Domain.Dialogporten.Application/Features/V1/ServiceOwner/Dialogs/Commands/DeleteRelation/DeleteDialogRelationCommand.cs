using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Relations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using FluentValidation;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.DeleteRelation;

public sealed class DeleteDialogRelationCommand : IRequest<DeleteDialogRelationResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid RelatedDialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
}

[GenerateOneOf]
public sealed partial class DeleteDialogRelationResult : OneOfBase<DeleteDialogRelationSuccess, EntityNotFound, ValidationError, Forbidden, DomainError, ConcurrencyError, Conflict>;

public sealed record DeleteDialogRelationSuccess(Guid Revision);

internal sealed class DeleteDialogRelationCommandValidator : AbstractValidator<DeleteDialogRelationCommand>
{
    public DeleteDialogRelationCommandValidator()
    {
        RuleFor(x => x.DialogId).NotEmpty();
        RuleFor(x => x.RelatedDialogId).NotEmpty();
    }
}

internal sealed class DeleteDialogRelationCommandHandler : IRequestHandler<DeleteDialogRelationCommand, DeleteDialogRelationResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogRelationWriter _relationWriter;
    private readonly IServiceOwnerDialogLoader _dialogLoader;

    public DeleteDialogRelationCommandHandler(
        IUnitOfWork unitOfWork,
        IDialogRelationWriter relationWriter,
        IServiceOwnerDialogLoader dialogLoader)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _relationWriter = relationWriter ?? throw new ArgumentNullException(nameof(relationWriter));
        _dialogLoader = dialogLoader ?? throw new ArgumentNullException(nameof(dialogLoader));
    }

    public async Task<DeleteDialogRelationResult> Handle(DeleteDialogRelationCommand request, CancellationToken cancellationToken)
    {
        // Either side's owner may remove a relation. The dialog in the path is the one that gets bumped.
        var dialog = await _dialogLoader.LoadDialog(request.DialogId, cancellationToken);
        if (dialog is null || !await _dialogLoader.OwnsEither(dialog, request.RelatedDialogId, cancellationToken))
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (request.IfMatchDialogRevision is { } revision && revision != dialog.Revision)
        {
            return new ConcurrencyError();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        if (!await _relationWriter.Remove(dialog, request.RelatedDialogId, cancellationToken))
        {
            return new EntityNotFound<DialogRelation>([request.DialogId, request.RelatedDialogId]);
        }

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<DeleteDialogRelationResult>(
            success => new DeleteDialogRelationSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }
}
