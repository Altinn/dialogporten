using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class GetDialogQuery : IRequest<GetDialogResult>
{
    public Guid DialogId { get; set; }

    /// <summary>
    /// Filter by end user id
    /// </summary>
    public string? EndUserId { get; init; }
}

[GenerateOneOf]
public sealed partial class GetDialogResult : OneOfBase<DialogDto, EntityNotFound, ValidationError>;

internal sealed class GetDialogQueryHandler : IRequestHandler<GetDialogQuery, GetDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;

    public GetDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization,
        IUnitOfWork unitOfWork, IUserRegistry userRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
    }

    public async Task<GetDialogResult> Handle(GetDialogQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        // This query could be written without all the includes as ProjectTo will do the job for us.
        // However, we need to guarantee an order for sub resources of the dialog aggregate.
        // This is to ensure that the get is consistent, and that PATCH in the API presentation
        // layer behaviours in an expected manner. Therefore we need to be a bit more verbose about it.
        var dialog = await _db.Dialogs
            .Include(x => x.Content.OrderBy(x => x.Id).ThenBy(x => x.CreatedAt))
                .ThenInclude(x => x.Value.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.SearchTags.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.DisplayName!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.Attachments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Urls.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Title!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.GuiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x!.Prompt!.Localizations.OrderBy(x => x.LanguageCode))
            .Include(x => x.ApiActions.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
                .ThenInclude(x => x.Endpoints.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Sender)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                .ThenInclude(x => x.Urls)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                .ThenInclude(x => x.DisplayName!.Localizations)
            .Include(x => x.Activities)
                .ThenInclude(x => x.Description!.Localizations)
            .Include(x => x.Activities)
                .ThenInclude(x => x.PerformedBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.SeenLog
                .Where(x => x.CreatedAt >= x.Dialog.UpdatedAt)
                .OrderBy(x => x.CreatedAt))
                .ThenInclude(x => x.SeenBy)
                .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.DialogEndUserContext)
            .Include(x => x.DialogServiceOwnerContext)
                .ThenInclude(x => x.ServiceOwnerLabels.OrderBy(x => x.Value))
            .IgnoreQueryFilters()
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var dialogDto = _mapper.Map<DialogDto>(dialog);

        if (request.EndUserId is not null)
        {
            var currentUserInformation = await _userRegistry.GetCurrentUserInformation(cancellationToken);

            var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(
                dialog,
                cancellationToken);

            if (!authorizationResult.HasAccessToMainResource())
            {
                return new EntityNotFound<DialogEntity>(request.DialogId);
            }

            dialog.UpdateSeenAt(
                currentUserInformation.UserId.ExternalIdWithPrefix,
                currentUserInformation.UserId.Type,
                currentUserInformation.Name);

            var saveResult = await _unitOfWork
                .DisableUpdatableFilter()
                .DisableVersionableFilter()
                .SaveChangesAsync(cancellationToken);

            saveResult.Switch(
                success => { },
                domainError => throw new UnreachableException("Should not get domain error when updating SeenAt."),
                concurrencyError => throw new UnreachableException("Should not get concurrencyError when updating SeenAt."));

            DecorateWithAuthorization(dialogDto, authorizationResult);
        }

        dialogDto.SeenSinceLastUpdate = dialog.SeenLog
            .Select(log =>
            {
                var logDto = _mapper.Map<DialogSeenLogDto>(log);
                logDto.IsViaServiceOwner = true;
                return logDto;
            })
            .ToList();

        return dialogDto;
    }

    private static void DecorateWithAuthorization(DialogDto dto,
        DialogDetailsAuthorizationResult authorizationResult)
    {
        foreach (var (action, resource) in authorizationResult.AuthorizedAltinnActions)
        {
            foreach (var apiAction in dto.ApiActions.Where(a => a.Action == action))
            {
                if ((apiAction.AuthorizationAttribute is null && resource == Constants.MainResource)
                    || (apiAction.AuthorizationAttribute is not null && resource == apiAction.AuthorizationAttribute))
                {
                    apiAction.IsAuthorized = true;
                }
            }

            foreach (var guiAction in dto.GuiActions.Where(a => a.Action == action))
            {
                if ((guiAction.AuthorizationAttribute is null && resource == Constants.MainResource)
                    || (guiAction.AuthorizationAttribute is not null && resource == guiAction.AuthorizationAttribute))
                {
                    guiAction.IsAuthorized = true;
                }
            }

            var authorizedTransmissions = dto.Transmissions.Where(t => authorizationResult.HasReadAccessToDialogTransmission(t.AuthorizationAttribute));
            foreach (var transmission in authorizedTransmissions)
            {
                transmission.IsAuthorized = true;
            }
        }
    }
}
