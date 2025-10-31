using AutoMapper;
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
    private readonly IDialogDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchActivityQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IAltinnAuthorization altinnAuthorization)
    {
        _dbContext = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<SearchActivityResult> Handle(SearchActivityQuery request, CancellationToken cancellationToken)
    {
        DialogEntity? dialog;
        await using (await _dbContext.BeginTransactionAsync(cancellationToken))
        {
            dialog = await _dbContext.Dialogs
                .Include(x => x.Activities)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: cancellationToken);
        }

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

        return _mapper.Map<List<ActivityDto>>(dialog.Activities);
    }
}
