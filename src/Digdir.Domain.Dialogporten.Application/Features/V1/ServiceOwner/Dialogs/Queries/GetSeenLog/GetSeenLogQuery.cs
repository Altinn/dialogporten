using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetSeenLog;

public sealed class GetSeenLogQuery : IRequest<GetSeenLogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid SeenLogId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetSeenLogResult : OneOfBase<SeenLogDto, EntityNotFound, EntityDeleted>;

internal sealed class GetSeenLogQueryHandler : IRequestHandler<GetSeenLogQuery, GetSeenLogResult>
{
    private readonly IMapper _mapper;
    private readonly IDialogDbContext _dbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public GetSeenLogQueryHandler(
        IMapper mapper,
        IDialogDbContext dbContext,
        IUserResourceRegistry userResourceRegistry)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<GetSeenLogResult> Handle(GetSeenLogQuery request,
        CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _dbContext.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .AsNoTracking()
                .Include(x => x.SeenLog.Where(x => x.Id == request.SeenLogId))
                    .ThenInclude(x => x.SeenBy)
                    .ThenInclude(x => x.ActorNameEntity)
                .IgnoreQueryFilters()
                .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(),
                    x => resourceIds.Contains(x.ServiceResource))
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: ct),
            cancellationToken);

        if (dialog is null)
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

        var dto = _mapper.Map<SeenLogDto>(seenLog);

        return dto;
    }
}
