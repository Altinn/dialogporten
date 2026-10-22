using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Relations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Relations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using FluentValidation;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchRelations;

public sealed class SearchDialogRelationsQuery : IRequest<SearchDialogRelationsResult>, IFeatureMetricServiceResourceThroughDialogIdRequest, IDialogRelationPagination
{
    public Guid DialogId { get; set; }

    /// <summary>
    /// Only return relations of this type, as seen from the dialog.
    /// </summary>
    public DialogRelationType.Values? Type { get; init; }

    /// <summary>
    /// Continuation token for pagination.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Limit the number of results returned. Defaults to 100, maximum 1000.
    /// </summary>
    public int? Limit { get; init; }
}

public sealed class DialogRelationDto
{
    /// <summary>
    /// The id of the related dialog.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid RelatedDialogId { get; set; }

    /// <summary>
    /// The type of the relation, as seen from the dialog in the path.
    /// </summary>
    public DialogRelationType.Values Type { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchDialogRelationsResult : OneOfBase<PaginatedList<DialogRelationDto>, EntityNotFound, ValidationError>;

internal sealed class SearchDialogRelationsQueryValidator : AbstractValidator<SearchDialogRelationsQuery>
{
    public SearchDialogRelationsQueryValidator()
    {
        RuleFor(x => x.DialogId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        Include(new DialogRelationPaginationValidator());
    }
}

internal sealed class SearchDialogRelationsQueryHandler : IRequestHandler<SearchDialogRelationsQuery, SearchDialogRelationsResult>
{
    private readonly IServiceOwnerDialogLoader _dialogLoader;
    private readonly IDialogRelationRepository _repository;

    public SearchDialogRelationsQueryHandler(IServiceOwnerDialogLoader dialogLoader, IDialogRelationRepository repository)
    {
        _dialogLoader = dialogLoader ?? throw new ArgumentNullException(nameof(dialogLoader));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<SearchDialogRelationsResult> Handle(SearchDialogRelationsQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _dialogLoader.LoadOwnedDialog(request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        // Not filtered by access: a relation may point at any dialog, including other service owners' dialogs.
        var limit = request.GetLimit();
        var relations = await _repository.GetRelations(dialog.Id, request.Type, request.GetAfter(), limit + 1, cancellationToken);

        return DialogRelationPagination.ToPage(
            relations,
            limit,
            x => x.RelatedDialogId,
            x => new DialogRelationDto { RelatedDialogId = x.RelatedDialogId, Type = x.Type });
    }
}
