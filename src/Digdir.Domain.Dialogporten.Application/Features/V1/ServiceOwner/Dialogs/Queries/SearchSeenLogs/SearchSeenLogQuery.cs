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

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchSeenLogs;

public sealed class SearchSeenLogQuery : IRequest<SearchSeenLogResult>, IFeatureMetricsServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchSeenLogResult : OneOfBase<List<SeenLogDto>, EntityNotFound, EntityDeleted>;

internal sealed class SearchSeenLogQueryHandler : IRequestHandler<SearchSeenLogQuery, SearchSeenLogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public SearchSeenLogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<SearchSeenLogResult> Handle(SearchSeenLogQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db.Dialogs
            .AsNoTracking()
            .Include(x => x.SeenLog)
                .ThenInclude(x => x.SeenBy)
                .ThenInclude(x => x.ActorNameEntity)
            .IgnoreQueryFilters()
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(),
                x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                cancellationToken: cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        return dialog.SeenLog
            .Select(x =>
            {
                var dto = _mapper.Map<SeenLogDto>(x);
                return dto;
            })
            .ToList();
    }
}
