using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
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
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRegistry _userRegistry;
    private readonly IDataLoaderContext _dataLoaderContext;
    private readonly IDialogSeenLogWriter _dialogSeenLogWriter;

    public GetDialogQueryHandler(
        IAltinnAuthorization altinnAuthorization,
        IUnitOfWork unitOfWork, IUserRegistry userRegistry,
        IDataLoaderContext dataLoaderContext,
        IDialogSeenLogWriter dialogSeenLogWriter
    )
    {
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(userRegistry);
        ArgumentNullException.ThrowIfNull(dataLoaderContext);

        _altinnAuthorization = altinnAuthorization;
        _unitOfWork = unitOfWork;
        _userRegistry = userRegistry;
        _dataLoaderContext = dataLoaderContext;
        _dialogSeenLogWriter = dialogSeenLogWriter;
    }

    public async Task<GetDialogResult> Handle(GetDialogQuery request, CancellationToken cancellationToken)
    {
        var dialog = GetDialogDataLoader.GetPreloadedData(_dataLoaderContext);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var dialogDto = dialog.ToDto();
        DialogSeenResult? seenResult = null;

        if (request.EndUserId is not null)
        {
            var authorizationResult = await _altinnAuthorization.GetDialogDetailsAuthorization(
                dialog,
                cancellationToken);

            if (!authorizationResult.HasAccessToMainResource())
            {
                return new EntityNotFound<DialogEntity>(request.DialogId);
            }

            var userId = _userRegistry.GetCurrentUserId();

            seenResult = await _dialogSeenLogWriter.OnSeen(dialog, userId, cancellationToken);

            if (seenResult != null)
            {
                var newSeenLog = seenResult.NewSeenLog;
                if (seenResult.CausedChangesOutsideEf) dialog.AddUpdateEvent();
                if (newSeenLog != null) dialog.AddSeenEvent(userId.ExternalIdWithPrefix, userId.Type, newSeenLog.Id);
            }

            var saveResult = await _unitOfWork
                .DisableAggregateFilter()
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
            request.EndUserId,
            seenResult?.NewSeenLog
        );

        dialogDto.SeenSinceLastContentUpdate = GetSeenLogs(
            dialog.SeenLog,
            dialog.ContentUpdatedAt,
            request.EndUserId,
            seenResult?.NewSeenLog
        );

        if (request.EndUserId is not null)
        {
            dialogDto.IsContentSeen = seenResult?.IsContentSeen ?? dialogDto.IsContentSeen;
            dialogDto.EndUserContext.SystemLabels.Remove(SystemLabel.Values.MarkedAsUnopened);
        }
        return dialogDto;
    }

    private static List<DialogSeenLogDto> GetSeenLogs(
        IEnumerable<DialogSeenLog> seenLogs,
        DateTimeOffset filterDate,
        string? endUserId,
        DialogSeenLog? newSeenLog) =>
        seenLogs
            .Where(log => log.CreatedAt >= filterDate)
            .Concat(newSeenLog is null ? [] : [newSeenLog])
            .GroupBy(log => log.SeenBy.ActorNameEntity!.ActorId)
            .Select(group => group
                .OrderByDescending(log => log.CreatedAt)
                .First()
            )
            .Select(log => ToSeenDialogDto(endUserId, log))
            .ToList();

    private static DialogSeenLogDto ToSeenDialogDto(string? endUserId, DialogSeenLog log)
        => log.ToDto(endUserId);

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

        dto.Content?.MainContentReference?.IsAuthorized = authorization.HasReadAccessToMainResource();

        foreach (var t in dto.Transmissions)
        {
            t.IsAuthorized = authorization.HasReadAccessToDialogTransmission(t.AuthorizationAttribute);
        }
    }
}
