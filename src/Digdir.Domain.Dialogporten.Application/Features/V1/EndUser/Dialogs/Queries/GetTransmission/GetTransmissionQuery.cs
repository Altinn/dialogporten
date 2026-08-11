#pragma warning disable CS0618 // Obsolete legacy authorization fields are mapped for backwards compatibility
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
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using static Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.AuthorizationExclusion;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetTransmission;

public sealed class GetTransmissionQuery : IRequest<GetTransmissionResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid TransmissionId { get; set; }
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class GetTransmissionResult : OneOfBase<TransmissionDto, EntityNotFound, EntityDeleted, Forbidden>;

internal sealed class GetTransmissionQueryHandler : IRequestHandler<GetTransmissionQuery, GetTransmissionResult>
{
    private const string ExcludedTransmission =
        "The transmission is not available to the authenticated user.";

    private readonly IDialogDbContext _dbContext;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IClock _clock;
    private readonly IDialogTokenGenerator _dialogTokenGenerator;

    public GetTransmissionQueryHandler(
        IDialogDbContext dbContext,
        IAltinnAuthorization altinnAuthorization,
        IClock clock,
        IDialogTokenGenerator dialogTokenGenerator)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(dialogTokenGenerator);

        _dbContext = dbContext;
        _altinnAuthorization = altinnAuthorization;
        _clock = clock;
        _dialogTokenGenerator = dialogTokenGenerator;
    }

    public async Task<GetTransmissionResult> Handle(GetTransmissionQuery request,
        CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .AsNoTracking()
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.Content)
                    .ThenInclude(x => x.Value.Localizations)
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.Attachments)
                    .ThenInclude(x => x.DisplayName!.Localizations)
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.NavigationalActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.Title.Localizations.OrderBy(x => x.LanguageCode))
                .Include(x => x.Transmissions)
                    .ThenInclude(x => x.Sender)
                    .ThenInclude(x => x.ActorNameEntity)
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.Transmissions.Where(x => x.Id == request.TransmissionId))
                    .ThenInclude(x => x.NavigationalActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .ThenInclude(x => x.AuthorizationContext)
                .Include(x => x.ServiceOwnerContext)
                    .ThenInclude(x => x.ServiceOwnerLabels)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                    cancellationToken: ct),
            cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(
            dialog,
            cancellationToken: cancellationToken);

        // If we cannot access the dialog at all, we don't allow access to any of the dialog transmissions.
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

        var transmission = dialog.Transmissions.FirstOrDefault();
        if (transmission is null)
        {
            return new EntityNotFound<DialogTransmission>(request.TransmissionId);
        }

        transmission.FilterTransmissionLocalizations(request.AcceptedLanguages);
        var dto = transmission.ToDto();

        var transmissionCheck = transmission.GetAuthorizationCheck(dialog);
        dto.IsAuthorized = authorizationResult.HasAccess(transmission, transmissionCheck);
        dto.ContextToken = _dialogTokenGenerator.GetContextTokenOrNull(dialog, authorizationResult, dto.IsAuthorized,
            transmission.AuthorizationContext, transmissionCheck,
            transmission.Id, DialogContextTokenEntityTypes.Transmission);

        if (!dto.IsAuthorized && transmission.ShouldExcludeWhenUnauthorized())
        {
            // 403 rather than the 404 the dialog-level check above returns: there, the dialog's very
            // existence is what is withheld, whereas an excluded transmission is published by the dialog's
            // own "excludedTransmissions" - denying that it exists would contradict the dialog response.
            return new Forbidden(ExcludedTransmission);
        }

        // Parent-first narrowing: transmission access is a precondition for its attachments and
        // navigational actions; a child context can only further restrict access. The DTO lists are
        // mapped 1:1 in order from the entity lists, so pairwise zipping is safe.
        foreach (var (attachmentDto, attachment) in dto.Attachments.Zip(transmission.Attachments))
        {
            var check = attachment.GetAuthorizationCheck(dialog);
            attachmentDto.IsAuthorized = authorizationResult.HasAccess(attachment, dto.IsAuthorized, check);
            attachmentDto.ContextToken = _dialogTokenGenerator.GetContextTokenOrNull(dialog, authorizationResult,
                attachmentDto.IsAuthorized, attachment.AuthorizationContext, check,
                attachment.Id, DialogContextTokenEntityTypes.TransmissionAttachment);
        }

        foreach (var (navigationalActionDto, navigationalAction) in dto.NavigationalActions.Zip(transmission.NavigationalActions))
        {
            var check = navigationalAction.GetAuthorizationCheck(dialog);
            navigationalActionDto.IsAuthorized = authorizationResult.HasAccess(navigationalAction, dto.IsAuthorized, check);
            navigationalActionDto.ContextToken = _dialogTokenGenerator.GetContextTokenOrNull(dialog, authorizationResult,
                navigationalActionDto.IsAuthorized, navigationalAction.AuthorizationContext, check,
                navigationalAction.Id, DialogContextTokenEntityTypes.TransmissionNavigationalAction);
        }

        // After the loops, so each DTO could still be paired with its entity by position above.
        (dto.Attachments, dto.ExcludedAttachments) =
            PartitionExcluded(dto.Attachments, transmission.Attachments, x => x.IsAuthorized);
        (dto.NavigationalActions, dto.ExcludedNavigationalActions) =
            PartitionExcluded(dto.NavigationalActions, transmission.NavigationalActions, x => x.IsAuthorized);

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
            dto.Content.ContentReference.ReplaceUnauthorizedContentReference();
            return dto;
        }

        ReplaceExpiredAttachmentUrls(dto);
        ReplaceExpiredNavigationalActionUrls(dto);
        return dto;
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
