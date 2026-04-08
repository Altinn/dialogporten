using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchActivities;

public sealed class SearchActivityQuery : IRequest<SearchActivityResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchActivityResult : OneOfBase<List<ActivityDto>, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class SearchActivityQueryHandler : IRequestHandler<SearchActivityQuery, SearchActivityResult>
{
    private readonly IDialogDbContext _db;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchActivityQueryHandler(
        IDialogDbContext db,
        IAltinnAuthorization altinnAuthorization)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);

        _db = db;
        _altinnAuthorization = altinnAuthorization;
    }

    public async Task<SearchActivityResult> Handle(SearchActivityQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .AsNoTracking()
                .Include(x => x.Activities)
                    .ThenInclude(x => x.Description!.Localizations)
                .Include(x => x.ServiceOwnerContext)
                    .ThenInclude(x => x.ServiceOwnerLabels)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: ct),
            cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(
            dialog,
            cancellationToken: cancellationToken);

        // If we cannot access the dialog at all, we don't allow access to any of the activity history
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

        dialog.FilterLocalizations(request.AcceptedLanguages);

        return dialog.Activities.Select(x => x.ToDto()).ToList();
    }
}
