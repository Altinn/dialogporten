using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLog;

public sealed class SearchLabelAssignmentLogQuery : IRequest<SearchLabelAssignmentLogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }

    public string? EndUserId { get; set; } // See ServiceOwnerOnBehalfOfPersonMiddleware
}

[GenerateOneOf]
public sealed partial class SearchLabelAssignmentLogResult : OneOfBase<List<LabelAssignmentLogDto>, EntityNotFound, EntityDeleted>;

internal sealed class SearchLabelAssignmentLogQueryHandler : IRequestHandler<SearchLabelAssignmentLogQuery, SearchLabelAssignmentLogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchLabelAssignmentLogQueryHandler(
        IDialogDbContext db,
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(userResourceRegistry);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);

        _db = db;
        _userResourceRegistry = userResourceRegistry;
        _altinnAuthorization = altinnAuthorization;
    }

    public async Task<SearchLabelAssignmentLogResult> Handle(SearchLabelAssignmentLogQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _userResourceRegistry.IsCurrentUserServiceOwnerAdmin();
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        var org = isAdmin ? null : await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);

        var dialog = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
                dbCtx.Dialogs
                    .AsNoTracking()
                    .Include(x => x.EndUserContext)
                        .ThenInclude(x => x.LabelAssignmentLogs)
                        .ThenInclude(x => x.PerformedBy)
                        .ThenInclude(x => x.ActorNameEntity)
                    .Include(x => x.ServiceOwnerContext)
                        .ThenInclude(x => x.ServiceOwnerLabels)
                    .IgnoreQueryFilters()
                    .WhereIf(!isAdmin, x => x.Org == org && resourceIds.Contains(x.ServiceResource))
                    .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken: ct),
            cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (request.EndUserId is not null)
        {
            // The dialog is owned by the service owner. Now check that the end user has access to it.
            var (hasAccess, _) = await _altinnAuthorization.GetDialogAccess(dialog, cancellationToken);
            if (!hasAccess)
            {
                return new EntityNotFound<DialogEntity>(request.DialogId);
            }
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        return dialog.EndUserContext
            .LabelAssignmentLogs
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.ToDto())
            .ToList();
    }
}
