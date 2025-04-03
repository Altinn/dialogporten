using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogActivities.Queries.Get;

public sealed class GetActivityQuery : IRequest<GetActivityResult>
{
    public Guid DialogId { get; set; }
    public Guid ActivityId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetActivityResult : OneOfBase<ActivityDto, EntityNotFound, EntityDeleted>;

internal sealed class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, GetActivityResult>
{
    private readonly IMapper _mapper;
    private readonly IDialogDbContext _dbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public GetActivityQueryHandler(IMapper mapper, IDialogDbContext dbContext, IUserResourceRegistry userResourceRegistry)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async ValueTask<GetActivityResult> Handle(GetActivityQuery request,
        CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _dbContext.Dialogs
            .Include(x => x.Activities.Where(x => x.Id == request.ActivityId))
                .ThenInclude(x => x.PerformedBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.Activities.Where(x => x.Id == request.ActivityId))
                .ThenInclude(x => x.Description!.Localizations)
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

        var activity = dialog.Activities.FirstOrDefault();

        if (activity is null)
        {
            return new EntityNotFound<DialogActivity>(request.ActivityId);
        }

        return _mapper.Map<ActivityDto>(activity);
    }
}
