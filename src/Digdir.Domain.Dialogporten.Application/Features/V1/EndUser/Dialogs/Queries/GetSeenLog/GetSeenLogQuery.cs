using System.Data;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;

public sealed class GetSeenLogQuery : IRequest<GetSeenLogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid SeenLogId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetSeenLogResult : OneOfBase<SeenLogDto, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class GetSeenLogQueryHandler : IRequestHandler<GetSeenLogQuery, GetSeenLogResult>
{
    private readonly IMapper _mapper;
    private readonly IDialogDbContext _dbContext;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUserRegistry _userRegistry;

    public GetSeenLogQueryHandler(
        IMapper mapper,
        IDialogDbContext dbContext,
        IAltinnAuthorization altinnAuthorization,
        IUserRegistry userRegistry)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
    }

    public async Task<GetSeenLogResult> Handle(GetSeenLogQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);


        DialogEntity? dialog;
        await using (await _dbContext.BeginTransactionAsync(cancellationToken))
        {
            dialog = await _dbContext.Dialogs
                .AsNoTracking()
                .Include(x => x.SeenLog.Where(x => x.Id == request.SeenLogId))
                    .ThenInclude(x => x.SeenBy)
                    .ThenInclude(x => x.ActorNameEntity)
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

        // If we cannot access the dialog at all, we don't allow access to the seen log
        if (!authorizationResult.HasAccessToMainResource())
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        var seenLog = dialog.SeenLog.FirstOrDefault();
        if (seenLog is null)
        {
            return new EntityNotFound<DialogSeenLog>(request.SeenLogId);
        }

        if (!await _altinnAuthorization.UserHasRequiredAuthLevel(dialog.ServiceResource, cancellationToken))
        {
            return new Forbidden(Constants.AltinnAuthLevelTooLow);
        }

        var dto = _mapper.Map<SeenLogDto>(seenLog);
        dto.IsCurrentEndUser = currentUserInformation.UserId.ExternalIdWithPrefix == seenLog.SeenBy.ActorNameEntity?.ActorId;

        return dto;
    }
}
