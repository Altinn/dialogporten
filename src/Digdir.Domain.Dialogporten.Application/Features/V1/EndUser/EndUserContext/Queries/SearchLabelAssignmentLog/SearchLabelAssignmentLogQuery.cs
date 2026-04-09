using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class SearchLabelAssignmentLogQuery : IRequest<SearchLabelAssignmentLogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchLabelAssignmentLogResult : OneOfBase<List<LabelAssignmentLogDto>, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class SearchLabelAssignmentLogQueryHandler : IRequestHandler<SearchLabelAssignmentLogQuery, SearchLabelAssignmentLogResult>
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchLabelAssignmentLogQueryHandler(IDialogDbContext dialogDbContext, IAltinnAuthorization altinnAuthorization)
    {
        ArgumentNullException.ThrowIfNull(dialogDbContext);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);

        _dialogDbContext = dialogDbContext;
        _altinnAuthorization = altinnAuthorization;
    }

    public async Task<SearchLabelAssignmentLogResult> Handle(SearchLabelAssignmentLogQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _dialogDbContext
            .WrapWithRepeatableRead((dbCtx, ct) =>
                dbCtx.Dialogs
                    .AsNoTracking()
                    .Include(x => x.EndUserContext)
                        .ThenInclude(x => x.LabelAssignmentLogs)
                        .ThenInclude(x => x.PerformedBy)
                        .ThenInclude(x => x.ActorNameEntity)
                    .Include(x => x.ServiceOwnerContext)
                        .ThenInclude(x => x.ServiceOwnerLabels)
                    .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                        cancellationToken: ct),
            cancellationToken);

        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(dialog, cancellationToken: cancellationToken);
        if (!authorizationResult.HasAccessToMainResource())
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (!await _altinnAuthorization.UserHasRequiredAuthLevel(dialog.ServiceResource, cancellationToken))
        {
            return new Forbidden(Constants.AltinnAuthLevelTooLow);
        }

        return dialog.EndUserContext
            .LabelAssignmentLogs
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.ToDto()).ToList();
    }
}
