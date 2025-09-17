using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class GetDialogQuery : IRequest<GetDialogResult>, IFeatureMetricsServiceResourceThroughDialogIdRequest
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
    private readonly IMapper _mapper;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IDataLoaderContext _dataLoaderContext;

    public GetDialogQueryHandler(
        IMapper mapper,
        IAltinnAuthorization altinnAuthorization,
        IUnitOfWork unitOfWork, IUserRegistry userRegistry,
        IDataLoaderContext dataLoaderContext)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _dataLoaderContext = dataLoaderContext ?? throw new ArgumentNullException(nameof(dataLoaderContext));
    }

    public async Task<GetDialogResult> Handle(GetDialogQuery request, CancellationToken cancellationToken)
    {
        var dialog = GetDialogDataLoader.GetPreloadedData(_dataLoaderContext);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        dialog.SeenLog = dialog.SeenLog
            .Where(x => x.CreatedAt >= dialog.ContentUpdatedAt).ToList();

        // Edge case where the dialog is requested the same instant it is purged
        // https://github.com/Altinn/dialogporten/issues/2627
        if (dialog.EndUserContext.DialogEndUserContextSystemLabels.Count == 0)
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


        dialogDto.SeenSinceLastUpdate = GetSeenLogs(
            dialog.SeenLog,
            dialog.UpdatedAt,
            request.EndUserId);

        dialogDto.SeenSinceLastContentUpdate = GetSeenLogs(
            dialog.SeenLog,
            dialog.ContentUpdatedAt,
            request.EndUserId);

        return dialogDto;
    }

    private List<DialogSeenLogDto> GetSeenLogs(
        IEnumerable<DialogSeenLog> seenLogs,
        DateTimeOffset filterDate,
        string? endUserId) =>
        seenLogs
            .Where(log => log.CreatedAt >= filterDate)
            .GroupBy(log => log.SeenBy.ActorNameEntity!.ActorId)
            .Select(group => group
                .OrderByDescending(log => log.CreatedAt)
                .First())
            .Select(log => ToSeenDialogDto(endUserId, log))
            .ToList();

    private DialogSeenLogDto ToSeenDialogDto(string? endUserId, DialogSeenLog log)
    {
        var actorId = log.SeenBy.ActorNameEntity?.ActorId;
        var logDto = _mapper.Map<DialogSeenLogDto>(log);
        logDto.IsCurrentEndUser = endUserId == actorId;
        return logDto;
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
