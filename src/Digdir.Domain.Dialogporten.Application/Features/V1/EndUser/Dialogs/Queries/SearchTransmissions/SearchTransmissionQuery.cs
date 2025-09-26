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

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;

public sealed class SearchTransmissionQuery : IRequest<SearchTransmissionResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchTransmissionResult : OneOfBase<List<TransmissionDto>, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class SearchTransmissionQueryHandler : IRequestHandler<SearchTransmissionQuery, SearchTransmissionResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchTransmissionQueryHandler(IDialogDbContext db, IMapper mapper, IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<SearchTransmissionResult> Handle(SearchTransmissionQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _db.Dialogs
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

        dialog.FilterLocalizations(request.AcceptedLanguages);

        var dto = _mapper.Map<List<TransmissionDto>>(dialog.Transmissions);

        foreach (var transmission in dto)
        {
            transmission.IsAuthorized = authorizationResult.HasReadAccessToDialogTransmission(transmission.AuthorizationAttribute);

            if (transmission.IsAuthorized) continue;

            var urls = transmission.Attachments.SelectMany(a => a.Urls).ToList();
            foreach (var url in urls)
            {
                url.Url = Constants.UnauthorizedUri;
            }
        }

        return dto;
    }
}
