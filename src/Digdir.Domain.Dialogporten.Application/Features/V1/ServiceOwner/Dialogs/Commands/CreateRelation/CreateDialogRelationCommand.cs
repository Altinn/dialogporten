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

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateRelation;

public sealed class CreateDialogRelationCommand : IRequest<CreateDialogRelationResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }

    /// <summary>
    /// The dialog to relate to. It must exist and not be deleted, but may belong to another party or service owner.
    /// </summary>
    public Guid RelatedDialogId { get; set; }

    /// <summary>
    /// The type of the relation, as seen from the dialog in the path. "Parent" means that the related dialog becomes
    /// the parent of this dialog.
    /// </summary>
    public DialogRelationType.Values Type { get; set; }
}

[GenerateOneOf]
public sealed partial class CreateDialogRelationResult : OneOfBase<CreateDialogRelationSuccess, EntityNotFound, EntityDeleted, ValidationError, Forbidden, DomainError, ConcurrencyError, Conflict>;

public sealed record CreateDialogRelationSuccess(Guid Revision);

internal sealed class CreateDialogRelationCommandValidator : AbstractValidator<CreateDialogRelationCommand>
{
    public CreateDialogRelationCommandValidator()
    {
        RuleFor(x => x.DialogId).NotEmpty();
        RuleFor(x => x.RelatedDialogId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
    }
}

internal sealed class CreateDialogRelationCommandHandler : IRequestHandler<CreateDialogRelationCommand, CreateDialogRelationResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogRelationWriter _relationWriter;
    private readonly IServiceOwnerDialogLoader _dialogLoader;

    public CreateDialogRelationCommandHandler(
        IUnitOfWork unitOfWork,
        IDialogRelationWriter relationWriter,
        IServiceOwnerDialogLoader dialogLoader)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _relationWriter = relationWriter ?? throw new ArgumentNullException(nameof(relationWriter));
        _dialogLoader = dialogLoader ?? throw new ArgumentNullException(nameof(dialogLoader));
    }

    public async Task<CreateDialogRelationResult> Handle(CreateDialogRelationCommand request, CancellationToken cancellationToken)
    {
        var dialog = await _dialogLoader.LoadOwnedDialog(request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (dialog.Frozen && !_dialogLoader.IsAdmin)
        {
            return new Forbidden("User cannot modify frozen dialog");
        }

        if (request.IfMatchDialogRevision is { } revision && revision != dialog.Revision)
        {
            return new ConcurrencyError();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        await _relationWriter.Add(
            dialog,
            isNewDialog: false,
            [new NewDialogRelation(request.RelatedDialogId, request.Type)],
            cancellationToken);

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<CreateDialogRelationResult>(
            success => new CreateDialogRelationSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }
}
