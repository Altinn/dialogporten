using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetActivity;

public sealed class GetActivityQuery : IRequest<GetActivityResult>, IFeatureMetricsServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid ActivityId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetActivityResult : OneOfBase<ActivityDto, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, GetActivityResult>
{
    private readonly IMapper _mapper;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogDbContext _dbContext;

    public GetActivityQueryHandler(
        IDialogDbContext dbContext,
        IMapper mapper,
        IAltinnAuthorization altinnAuthorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<GetActivityResult> Handle(GetActivityQuery request,
        CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.Dialogs
            .Include(x => x.Activities.Where(x => x.Id == request.ActivityId))
                .ThenInclude(x => x.PerformedBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.Activities.Where(x => x.Id == request.ActivityId))
                .ThenInclude(x => x.Description!.Localizations)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                cancellationToken: cancellationToken);

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

        var activity = dialog.Activities.FirstOrDefault();

        if (activity is null)
        {
            return new EntityNotFound<DialogActivity>(request.ActivityId);
        }

        return _mapper.Map<ActivityDto>(activity);
    }
}
