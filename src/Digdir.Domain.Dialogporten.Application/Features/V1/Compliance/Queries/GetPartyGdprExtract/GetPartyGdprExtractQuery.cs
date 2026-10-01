using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.Activities;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.LabelAssignmentLogs;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.SeenLogs;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.Authorization.AuthorizationExclusion;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract;

public sealed class PartyGdprExtractQuery : IRequest<PartyGdprExtractResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public string PartyId { get; set; } = null!;
}

[GenerateOneOf]
public sealed partial class PartyGdprExtractResult : OneOfBase<PartyGdprExtractDto>;

internal sealed class GetPartyGdprExtractQueryHandler(
    IClock clock,
    IDialogDbContext dialogDbContext,
    IAltinnAuthorization altinnAuthorization
) : IRequestHandler<PartyGdprExtractQuery, PartyGdprExtractResult>
{
    public async Task<PartyGdprExtractResult> Handle(PartyGdprExtractQuery request, CancellationToken cancellationToken)
    {
        var pid = request.PartyId.Split(':').Last();

        var dialogEntities = await dialogDbContext.WrapWithRepeatableRead((dbCtx, ct) => dbCtx.Dialogs
                .IncludeFullDialogAggregate()
                .IgnoreQueryFilters()
                .Where(x => x.Party == request.PartyId)
                .ToListAsync(ct),
            cancellationToken
        );

        var dialogDtos = new List<DialogDto>();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity( // Todo: GetDialogDetailsAuthorization should abstract this.
            [
                new Claim(ClaimsPrincipalExtensions.PidClaim, pid),
            ], "GdprExtract"));

        foreach (var dialog in dialogEntities)
        {
            var (hasAccess, authorization) = await altinnAuthorization.GetDialogAccess(
                principal,
                dialog,
                cancellationToken
            );
            if (!hasAccess) continue;

            var dialogDto = dialog.ToGdprExtractDto();

            DecorateWithAuthorization(dialog, dialogDto, authorization);
            ApplyExclusion(dialog, dialogDto);
            ReplaceUnauthorizedUrls(dialogDto);
            ReplaceExpiredAttachmentUrls(dialogDto);
            dialogDtos.Add(dialogDto);
        }

        var seenLogs = dialogDbContext.DialogSeenLog
            .Include(x => x.SeenBy).ThenInclude(x => x.ActorNameEntity)
            .Where(x => x.SeenBy.ActorNameEntity!.ActorId == request.PartyId)
            .ToList();

        var activities = dialogDbContext.DialogActivities
            .Include(x => x.PerformedBy).ThenInclude(x => x.ActorNameEntity)
            .Where(x => x.PerformedBy.ActorNameEntity!.ActorId == request.PartyId);

        var labelAssignmentLogs = dialogDbContext.LabelAssignmentLogs
            .Include(x => x.PerformedBy).ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.Context)
            .Where(x => x.PerformedBy.ActorNameEntity!.ActorId == request.PartyId);


        return new PartyGdprExtractDto(
            dialogDtos,
            seenLogs.ToDtoList(x => x.ToGdprExtractDto()),
            activities.ToDtoList(x => x.ToGdprExtractDto()),
            labelAssignmentLogs.ToDtoList(x => x.ToGdprExtractDto())
        );
    }

    // Authorization is evaluated against the domain entities (which carry the authorization contexts);
    // the DTO lists are mapped 1:1 in order from the entity lists, so pairwise zipping is safe.
    // Returns the dialog token's authorized entity references: for every context-carrying entity the user is
    // authorized for, the context's token reference or the entity's id. The dialog token's action claim stays
    // frozen at legacy semantics; context grants are expressed exclusively through these references.
    private static void DecorateWithAuthorization(
        DialogEntity dialog,
        DialogDto dto,
        DialogDetailsAuthorizationResult authorization
    )
    {
        foreach (var (a, apiAction) in dto.ApiActions.Zip(dialog.ApiActions))
        {
            a.IsAuthorized = authorization.HasAccess(apiAction, apiAction.GetAuthorizationCheck(dialog));
        }

        foreach (var (g, guiAction) in dto.GuiActions.Zip(dialog.GuiActions))
        {
            g.IsAuthorized = authorization.HasAccess(guiAction, guiAction.GetAuthorizationCheck(dialog));
        }

        dto.Content.MainContentReference?.IsAuthorized = authorization.HasReadAccessToMainResource();

        foreach (var (a, attachment) in dto.Attachments.Zip(dialog.Attachments))
        {
            a.IsAuthorized = authorization.HasAccess(attachment, attachment.GetAuthorizationCheck(dialog));
        }

        foreach (var (t, transmission) in dto.Transmissions.Zip(dialog.Transmissions))
        {
            t.IsAuthorized = authorization.HasAccess(transmission, transmission.GetAuthorizationCheck(dialog));

            // Parent-first narrowing: transmission access is a precondition for its attachments and
            // navigational actions; a child context can only further restrict access.
            foreach (var (a, attachment) in t.Attachments.Zip(transmission.Attachments))
            {
                a.IsAuthorized = authorization.HasAccess(attachment, t.IsAuthorized,
                    attachment.GetAuthorizationCheck(dialog));
            }

            foreach (var (n, navigationalAction) in t.NavigationalActions.Zip(transmission.NavigationalActions))
            {
                n.IsAuthorized = authorization.HasAccess(navigationalAction, t.IsAuthorized,
                    navigationalAction.GetAuthorizationCheck(dialog));
            }
        }
    }

    // Entities whose authorization context asks for unauthorizedPresentation = excluded are removed from
    // the collection they belong to when the user is not authorized, and recorded in the sibling "excluded"
    // list as id and creation time only. Excluding a transmission takes its children with it.
    private static void ApplyExclusion(DialogEntity dialog, DialogDto dto)
    {
        (dto.ApiActions, dto.ExcludedApiActions) = PartitionExcluded(dto.ApiActions, dialog.ApiActions,
            x => x.IsAuthorized);
        (dto.GuiActions, dto.ExcludedGuiActions) = PartitionExcluded(dto.GuiActions, dialog.GuiActions,
            x => x.IsAuthorized);
        (dto.Attachments, dto.ExcludedAttachments) = PartitionExcluded(dto.Attachments, dialog.Attachments,
            x => x.IsAuthorized);

        foreach (var (t, transmission) in dto.Transmissions.Zip(dialog.Transmissions))
        {
            (t.Attachments, t.ExcludedAttachments) = PartitionExcluded(t.Attachments, transmission.Attachments,
                x => x.IsAuthorized);
            (t.NavigationalActions, t.ExcludedNavigationalActions) = PartitionExcluded(t.NavigationalActions,
                transmission.NavigationalActions, x => x.IsAuthorized);
        }

        // Last, so the loop above can still pair transmission DTOs with their entities by position.
        (dto.Transmissions, dto.ExcludedTransmissions) = PartitionExcluded(dto.Transmissions, dialog.Transmissions,
            x => x.IsAuthorized);
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

        foreach (var url in dto.Attachments.Where(a => !a.IsAuthorized).SelectMany(a => a.Urls))
        {
            url.Url = Constants.UnauthorizedUri;
        }

        foreach (var dialogTransmission in dto.Transmissions.Where(e => !e.IsAuthorized))
        {
            dialogTransmission.Content?.ContentReference.ReplaceUnauthorizedContentReference();
        }

        // Covers both children of unauthorized transmissions (never individually authorized) and
        // individually unauthorized children within authorized transmissions.
        foreach (var dialogTransmission in dto.Transmissions)
        {
            foreach (var url in dialogTransmission.Attachments.Where(a => !a.IsAuthorized).SelectMany(a => a.Urls))
            {
                url.Url = Constants.UnauthorizedUri;
            }

            foreach (var action in dialogTransmission.NavigationalActions.Where(a => !a.IsAuthorized))
            {
                action.Url = Constants.UnauthorizedUri;
            }
        }
    }

    private void ReplaceExpiredAttachmentUrls(DialogDto dto)
    {
        var expiredDialogAttachmentUrls = dto.Attachments
            .Where(x => x.IsAuthorized)
            .Where(x => x.ExpiresAt < clock.UtcNowOffset)
            .SelectMany(x => x.Urls);

        foreach (var url in expiredDialogAttachmentUrls)
        {
            url.Url = Constants.ExpiredUri;
        }

        var expiredTransmissionAttachmentUrls = dto.Transmissions
            .Where(x => x.IsAuthorized)
            .SelectMany(x => x.Attachments)
            .Where(x => x.IsAuthorized)
            .Where(x => x.ExpiresAt < clock.UtcNowOffset)
            .SelectMany(x => x.Urls);

        foreach (var url in expiredTransmissionAttachmentUrls)
        {
            url.Url = Constants.ExpiredUri;
        }

        var expiredTransmissionNavigationalActions = dto.Transmissions
            .Where(x => x.IsAuthorized)
            .SelectMany(x => x.NavigationalActions)
            .Where(x => x.IsAuthorized)
            .Where(x => x.ExpiresAt < clock.UtcNowOffset);

        foreach (var action in expiredTransmissionNavigationalActions)
        {
            action.Url = Constants.ExpiredUri;
        }
    }
}
