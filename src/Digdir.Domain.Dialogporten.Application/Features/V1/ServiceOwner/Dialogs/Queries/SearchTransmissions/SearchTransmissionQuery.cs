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

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;

public sealed class SearchTransmissionQuery : IRequest<SearchTransmissionResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchTransmissionResult : OneOfBase<List<TransmissionDto>, EntityNotFound, EntityDeleted>;

internal sealed class SearchTransmissionQueryHandler : IRequestHandler<SearchTransmissionQuery, SearchTransmissionResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public SearchTransmissionQueryHandler(IDialogDbContext db, IMapper mapper, IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<SearchTransmissionResult> Handle(SearchTransmissionQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        DialogEntity? dialog;
        await using (await _db.BeginTransactionAsync(cancellationToken))
        {
            dialog = await _db.Dialogs
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Content.OrderBy(x => x.Id).ThenBy(x => x.CreatedAt))
                    .ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Sender)
                    .ThenInclude(x => x.ActorNameEntity)
                .IgnoreQueryFilters()
                .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(),
                    x => resourceIds.Contains(x.ServiceResource))
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: cancellationToken);
        }

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        return _mapper.Map<List<TransmissionDto>>(dialog.Transmissions);
    }
}
