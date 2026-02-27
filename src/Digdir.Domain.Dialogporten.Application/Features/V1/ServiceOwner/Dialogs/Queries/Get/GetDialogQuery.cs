using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class GetDialogQuery : IRequest<GetDialogResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
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
                concurrencyError => throw new UnreachableException("Should not get concurrencyError when updating SeenAt."),
                conflict => throw new UnreachableException("Should not get conflict when updating SeenAt.")
            );

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
        DialogDetailsAuthorizationResult authorization)
    {
        foreach (var a in dto.ApiActions)
        {
            a.IsAuthorized = authorization.HasAccessToApiAction(a.Action, a.AuthorizationAttribute);
        }

        foreach (var g in dto.GuiActions)
        {
            g.IsAuthorized = authorization.HasAccessToGuiAction(g.Action, g.AuthorizationAttribute);
        }

        dto.Content?.MainContentReference?.IsAuthorized = authorization.HasReadAccessToMainResource();

        foreach (var t in dto.Transmissions)
        {
            t.IsAuthorized = authorization.HasReadAccessToDialogTransmission(t.AuthorizationAttribute);
        }
    }
}
