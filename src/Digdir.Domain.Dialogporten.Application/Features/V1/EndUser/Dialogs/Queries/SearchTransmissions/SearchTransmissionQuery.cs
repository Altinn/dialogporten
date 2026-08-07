#pragma warning disable CS0618 // Obsolete legacy authorization fields are cleared during redaction
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
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
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IClock _clock;

    public SearchTransmissionQueryHandler(IDialogDbContext db, IAltinnAuthorization altinnAuthorization, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(clock);

        _db = db;
        _altinnAuthorization = altinnAuthorization;
        _clock = clock;
    }

    public async Task<SearchTransmissionResult> Handle(SearchTransmissionQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .AsNoTracking()
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
                    .ThenInclude(x => x.NavigationalActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.Title.Localizations.OrderBy(x => x.LanguageCode))
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Sender)
                    .ThenInclude(x => x.ActorNameEntity)
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.NavigationalActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.ServiceOwnerContext)
                    .ThenInclude(x => x.ServiceOwnerLabels)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: ct), cancellationToken);

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

        dialog.FilterDialogLocalizations(request.AcceptedLanguages);

        var transmissions = dialog.Transmissions
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList();

        var dtos = new List<TransmissionDto>(transmissions.Count);
        foreach (var transmission in transmissions)
        {
            var dto = transmission.ToDto();
            dto.IsAuthorized = authorizationResult.HasAccess(transmission, dialog);

            if (!dto.IsAuthorized && transmission.ShouldRedactWhenUnauthorized())
            {
                RedactTransmission(dto);
                dtos.Add(dto);
                continue;
            }

            // Parent-first narrowing: transmission access is a precondition for its attachments and
            // navigational actions; a child context can only further restrict access. The DTO lists are
            // mapped 1:1 in order from the entity lists, so pairwise zipping is safe.
            foreach (var (attachmentDto, attachment) in dto.Attachments.Zip(transmission.Attachments))
            {
                attachmentDto.IsAuthorized = authorizationResult.HasAccess(attachment, dto.IsAuthorized, dialog);
                if (!attachmentDto.IsAuthorized && attachment.ShouldRedactWhenUnauthorized())
                {
                    attachmentDto.DisplayName = [];
                    attachmentDto.Name = null;
                    attachmentDto.Urls = [];
                }
            }

            foreach (var (navigationalActionDto, navigationalAction) in dto.NavigationalActions.Zip(transmission.NavigationalActions))
            {
                navigationalActionDto.IsAuthorized = authorizationResult.HasAccess(navigationalAction, dto.IsAuthorized, dialog);
                if (!navigationalActionDto.IsAuthorized && navigationalAction.ShouldRedactWhenUnauthorized())
                {
                    navigationalActionDto.Title = [];
                }
            }

            foreach (var url in dto.Attachments.Where(a => !a.IsAuthorized).SelectMany(a => a.Urls))
            {
                url.Url = Constants.UnauthorizedUri;
            }

            foreach (var action in dto.NavigationalActions.Where(a => !a.IsAuthorized))
            {
                action.Url = Constants.UnauthorizedUri;
            }

            if (!dto.IsAuthorized)
            {
                dto.Content?.ContentReference.ReplaceUnauthorizedContentReference();
            }
            else
            {
                ReplaceExpiredAttachmentUrls(dto);
                ReplaceExpiredNavigationalActionUrls(dto);
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    // Strips all content from a transmission the user is not authorized to see, leaving only its
    // existence and timestamps.
    private static void RedactTransmission(TransmissionDto transmission)
    {
        transmission.Sender = null;
        transmission.Content = null;
        transmission.ExtendedType = null;
        transmission.ExternalReference = null;
        transmission.AuthorizationAttribute = null;
        transmission.RelatedTransmissionId = null;
        transmission.Attachments = [];
        transmission.NavigationalActions = [];
    }

    private void ReplaceExpiredAttachmentUrls(TransmissionDto dto)
    {
        var expiredTransmissionAttachmentUrls = dto
            .Attachments
            .Where(x => x.IsAuthorized)
            .Where(x => x.ExpiresAt < _clock.UtcNowOffset)
            .SelectMany(x => x.Urls);

        foreach (var url in expiredTransmissionAttachmentUrls)
        {
            url.Url = Constants.ExpiredUri;
        }
    }

    private void ReplaceExpiredNavigationalActionUrls(TransmissionDto dto)
    {
        var expiredNavigationalActions = dto.NavigationalActions
            .Where(x => x.IsAuthorized)
            .Where(x => x.ExpiresAt < _clock.UtcNowOffset);

        foreach (var action in expiredNavigationalActions)
        {
            action.Url = Constants.ExpiredUri;
        }
    }
}
