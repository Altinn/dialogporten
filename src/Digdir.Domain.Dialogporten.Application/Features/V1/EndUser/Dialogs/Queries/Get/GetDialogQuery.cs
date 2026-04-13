using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.Authorization.Constants;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;

public sealed class GetDialogQuery : IRequest<GetDialogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class GetDialogResult : OneOfBase<DialogDto, EntityNotFound, EntityNotVisible, EntityDeleted, Forbidden>;

internal sealed class GetDialogQueryHandler : IRequestHandler<GetDialogQuery, GetDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IClock _clock;
    private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogTokenGenerator _dialogTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public GetDialogQueryHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IClock clock,
        IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization,
        IDialogTokenGenerator dialogTokenGenerator)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(userRegistry);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(dialogTokenGenerator);

        _db = db;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _userRegistry = userRegistry;
        _altinnAuthorization = altinnAuthorization;
        _dialogTokenGenerator = dialogTokenGenerator;
    }

    public async Task<GetDialogResult> Handle(GetDialogQuery request, CancellationToken cancellationToken)
    {
        // This query could be written without all the includes as ProjectTo will do the job for us.
        // However, we need to guarantee an order for sub resources of the dialog aggregate.
        // This is to ensure that the get is consistent, and that PATCH in the API presentation
        // layer behaviours in an expected manner. Therefore, we need to be a bit more verbose about it.
        var dialog = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
                dbCtx.Dialogs
                    .Include(x => x.Content)
                        .ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Title!.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Prompt!.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.ApiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Endpoints.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .Include(x => x.Transmissions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Content.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.Transmissions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Sender)
                        .ThenInclude(x => x.ActorNameEntity)
                    .Include(x => x.Transmissions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                    .Include(x => x.Transmissions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.Transmissions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.NavigationalActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                        .ThenInclude(x => x.Title.Localizations.OrderBy(x => x.LanguageCode))
                    .Include(x => x.Activities)
                        .ThenInclude(x => x.Description!.Localizations)
                    .Include(x => x.Activities)
                        .ThenInclude(x => x.PerformedBy)
                        .ThenInclude(x => x.ActorNameEntity)
                    .Include(x => x.SeenLog
                        .Where(x => x.CreatedAt >= x.Dialog.ContentUpdatedAt)
                        .OrderBy(x => x.CreatedAt))
                        .ThenInclude(x => x.SeenBy)
                        .ThenInclude(x => x.ActorNameEntity)
                    .Include(x => x.EndUserContext)
                        .ThenInclude(x => x.DialogEndUserContextSystemLabels)
                    .Include(x => x.ServiceOwnerContext)
                        .ThenInclude(x => x.ServiceOwnerLabels)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == request.DialogId, ct),
            cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(
            dialog,
            cancellationToken: cancellationToken);

        if (!authorizationResult.HasAccessToMainResource())
        {
            // If the user for some reason does not have access to the main resource, which might be
            // because they are granted access to XACML-actions besides "read" not explicitly defined in the dialog,
            // we do a recheck if the user has access to the dialog via the list authorization. If this is the case,
            // we return the dialog and let DecorateWithAuthorization flag the actions as unauthorized. Note that
            // there might be transmissions that the user has access to, even though there are no authorized actions.
            var listAuthorizationResult = await _altinnAuthorization.HasListAuthorizationForDialog(
                dialog,
                cancellationToken: cancellationToken);

            if (!listAuthorizationResult)
            {
                return new EntityNotFound<DialogEntity>(request.DialogId);
            }
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (!await _altinnAuthorization.UserHasRequiredAuthLevel(dialog.ServiceResource, cancellationToken))
        {
            return new Forbidden(Constants.AltinnAuthLevelTooLow);
        }

        if (dialog.VisibleFrom.HasValue && dialog.VisibleFrom > _clock.UtcNowOffset)
        {
            return new EntityNotVisible<DialogEntity>(dialog.VisibleFrom.Value);
        }

        var userId = _userRegistry.GetCurrentUserId();

        dialog.OnSeen(userId.ExternalIdWithPrefix, userId.Type);

        var saveResult = await _unitOfWork
            .DisableUpdatableFilter()
            .DisableVersionableFilter()
            .SaveChangesAsync(cancellationToken);

        saveResult.Switch(
            success => { },
            domainError => throw new UnreachableException("Should not get domain error when updating SeenAt."),
            concurrencyError =>
                throw new UnreachableException("Should not get concurrencyError when updating SeenAt."),
            conflict => throw new UnreachableException("Should not get conflict when updating SeenAt."));


        dialog.FilterLocalizations(request.AcceptedLanguages);

        var dialogDto = _mapper.Map<DialogDto>(dialog);

        dialogDto.SeenSinceLastUpdate = GetSeenLogs(
            dialog.SeenLog,
            dialog.UpdatedAt,
            userId.ExternalIdWithPrefix);

        dialogDto.SeenSinceLastContentUpdate = GetSeenLogs(
            dialog.SeenLog,
            dialog.ContentUpdatedAt,
            userId.ExternalIdWithPrefix);

        dialogDto.DialogToken = _dialogTokenGenerator.GetDialogToken(
            dialog,
            authorizationResult,
            DialogTokenIssuerVersion
        );

        DecorateWithAuthorization(dialogDto, authorizationResult);
        ReplaceUnauthorizedUrls(dialogDto);
        ReplaceExpiredAttachmentUrls(dialogDto);

        dialogDto.EndUserContext.SystemLabels.Remove(SystemLabel.Values.MarkedAsUnopened);

        return dialogDto;
    }

    private List<DialogSeenLogDto> GetSeenLogs(
        IEnumerable<DialogSeenLog> seenLogs,
        DateTimeOffset filterDate,
        string externalId) =>
        seenLogs
            .Where(log => log.CreatedAt >= filterDate)
            .GroupBy(log => log.SeenBy.ActorNameEntity!.ActorId)
            .Select(group => group
                .OrderByDescending(log => log.CreatedAt)
                .First())
            .Select(log => ToSeenLogDto(externalId, log))
            .ToList();

    private DialogSeenLogDto ToSeenLogDto(string externalId, DialogSeenLog log)
    {
        var actorId = log.SeenBy.ActorNameEntity?.ActorId;
        var logDto = _mapper.Map<DialogSeenLogDto>(log);
        logDto.IsCurrentEndUser = externalId == actorId;
        return logDto;
    }

    private static void DecorateWithAuthorization(DialogDto dto,
        DialogDetailsAuthorizationResult authorization)
    {
        foreach (var a in dto.ApiActions)
        {
            a.IsAuthorized = authorization.HasAccessToAction(a.Action, a.AuthorizationAttribute);
        }

        foreach (var g in dto.GuiActions)
        {
            g.IsAuthorized = authorization.HasAccessToAction(g.Action, g.AuthorizationAttribute);
        }

        dto.Content.MainContentReference?.IsAuthorized = authorization.HasReadAccessToMainResource();

        foreach (var t in dto.Transmissions)
        {
            t.IsAuthorized = authorization.HasReadAccessToDialogTransmission(t.AuthorizationAttribute);
        }
    }

    private static void ReplaceUnauthorizedUrls(DialogDto dto)
    {
        // For all API and GUI actions and transmissions where isAuthorized is false, replace the URLs with Constants.UnauthorizedUrl
        foreach (var guiAction in dto.GuiActions.Where(a => !a.IsAuthorized))
        {
            guiAction.Url = Constants.UnauthorizedUri;
        }

        foreach (var apiAction in dto.ApiActions.Where(a => !a.IsAuthorized))
        {
            foreach (var endpoint in apiAction.Endpoints)
            {
                endpoint.Url = Constants.UnauthorizedUri;
            }
        }

        if (dto.Content.MainContentReference?.IsAuthorized == false)
        {
            dto.Content.MainContentReference.ReplaceUnauthorizedContentReference();
        }

        foreach (var dialogTransmission in dto.Transmissions.Where(e => !e.IsAuthorized))
        {
            dialogTransmission.Content.ContentReference.ReplaceUnauthorizedContentReference();
            var urls = dialogTransmission.Attachments.SelectMany(a => a.Urls).ToList();
            foreach (var url in urls)
            {
                url.Url = Constants.UnauthorizedUri;
            }

            foreach (var action in dialogTransmission.NavigationalActions)
            {
                action.Url = Constants.UnauthorizedUri;
            }
        }
    }

    private void ReplaceExpiredAttachmentUrls(DialogDto dto)
    {
        var expiredDialogAttachmentUrls = dto.Attachments
            .Where(x => x.ExpiresAt < _clock.UtcNowOffset)
            .SelectMany(x => x.Urls);

        foreach (var url in expiredDialogAttachmentUrls)
        {
            url.Url = Constants.ExpiredUri;
        }

        var expiredTransmissionAttachmentUrls = dto.Transmissions
            .Where(x => x.IsAuthorized)
            .SelectMany(x => x.Attachments)
            .Where(x => x.ExpiresAt < _clock.UtcNowOffset)
            .SelectMany(x => x.Urls);

        foreach (var url in expiredTransmissionAttachmentUrls)
        {
            url.Url = Constants.ExpiredUri;
        }

        var expiredTransmissionNavigationalActions = dto.Transmissions
            .Where(x => x.IsAuthorized)
            .SelectMany(x => x.NavigationalActions)
            .Where(x => x.ExpiresAt < _clock.UtcNowOffset);

        foreach (var action in expiredTransmissionNavigationalActions)
        {
            action.Url = Constants.ExpiredUri;
        }
    }
}
