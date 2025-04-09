using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using OneOf;
using Microsoft.EntityFrameworkCore;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Mediator;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSeenLogs.Queries.Search;

public sealed class SearchSeenLogQuery : IRequest<SearchSeenLogResult>
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchSeenLogResult : OneOfBase<List<SeenLogDto>, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class SearchSeenLogQueryHandler : IRequestHandler<SearchSeenLogQuery, SearchSeenLogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUserRegistry _userRegistry;

    public SearchSeenLogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IAltinnAuthorization altinnAuthorization,
        IUserRegistry userRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
    }

    public async ValueTask<SearchSeenLogResult> Handle(SearchSeenLogQuery request, CancellationToken cancellationToken)
    {
        var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);

        var dialog = await _db.Dialogs
            .AsNoTracking()
            .Include(x => x.SeenLog)
                .ThenInclude(x => x.SeenBy)
                .ThenInclude(x => x.ActorNameEntity)
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

        // If we cannot access the dialog at all, we don't allow access to the seen log
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

        return dialog.SeenLog
            .Select(x =>
            {
                var dto = _mapper.Map<SeenLogDto>(x);
                dto.IsCurrentEndUser = currentUserInformation.UserId.ExternalIdWithPrefix == x.SeenBy.ActorNameEntity?.ActorId;
                return dto;
            })
            .ToList();
    }
}
