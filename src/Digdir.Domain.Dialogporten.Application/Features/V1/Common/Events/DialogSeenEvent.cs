using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserIdType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;

public sealed class DialogSeenEvent(
    IPartyNameRegistry partyNameRegistry,
    IDialogDbContext db,
    IUnitOfWork unitOfWork
) : INotificationHandler<DialogSeenDomainEvent>
{
    private readonly IPartyNameRegistry _partyNameRegistry = partyNameRegistry ?? throw new ArgumentNullException(nameof(partyNameRegistry));
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task Handle(DialogSeenDomainEvent dialogSeenDomainEvent, CancellationToken cancellationToken)
    {
        var userId = new UserId
        {
            Type = dialogSeenDomainEvent.UserType,
            ExternalId = dialogSeenDomainEvent.UserId,
        };
        var normalizedActorId = userId.ExternalId.ToLowerInvariant();

        var name = userId.Type switch
        {
            UserIdType.Person or
                UserIdType.ServiceOwnerOnBehalfOfPerson or
                UserIdType.AltinnSelfIdentifiedUser or
                UserIdType.IdportenEmailIdentifiedUser or
                UserIdType.FeideUser or
                UserIdType.SystemUser => await _partyNameRegistry.GetName(userId.ExternalIdWithPrefix, cancellationToken),
            UserIdType.Unknown or
                UserIdType.ServiceOwner => throw new UnreachableException(),
            _ => throw new UnreachableException()
        };

        var dialog = await _db.Dialogs
            .Include(x => x.SeenLog
                .Where(log => log.CreatedAt >= log.Dialog.ContentUpdatedAt)
                .OrderBy(log => log.CreatedAt))
            .ThenInclude(x => x.SeenBy)
            .ThenInclude(x => x.ActorNameEntity)
            .Include(x => x.EndUserContext)
            .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == dialogSeenDomainEvent.DialogId, cancellationToken);

        if (dialog == null)
        {
            return;
        }

        var lastSeen = dialog.SeenLog
            .Where(x => x.SeenBy.ActorNameEntity?.ActorId == userId.ExternalId)
            .MaxBy(x => x.CreatedAt);

        if (lastSeen is not null && lastSeen.Id != dialogSeenDomainEvent.LastSeenId)
        {
            return;
        }

        var actorNameEntity = await _db.ActorName
                .FirstOrDefaultAsync(
                    x => x.ActorId == normalizedActorId && x.Name == name,
                    cancellationToken)
         ?? new ActorName { Name = name, ActorId = normalizedActorId };

        // Use a deterministic id to make the seen-log insert idempotent.
        // If multiple seen events are produced without dialog changes in between,
        // they represent the same logical "seen" and should not create duplicates.
        var id = dialogSeenDomainEvent.DialogId
            .CreateDeterministicSubUuidV7($"{userId.ExternalId}{(dialogSeenDomainEvent.LastSeenId is not null ? dialogSeenDomainEvent.LastSeenId.ToString() : "")}");

        var dialogSeenLogSeenByActor = new DialogSeenLogSeenByActor
        {
            ActorTypeId = ActorType.Values.PartyRepresentative,
            ActorNameEntity = actorNameEntity
        };


        var seenLog = new DialogSeenLog
        {
            Id = id,
            EndUserTypeId = userId.Type,
            IsViaServiceOwner = userId.Type == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson,
            SeenBy = dialogSeenLogSeenByActor,
            CreatedAt = dialogSeenDomainEvent.OccurredAt
        };

        var performedBy = LabelAssignmentLogActorFactory.Create(
            dialogSeenLogSeenByActor.ActorTypeId,
            actorNameEntity.ActorId,
            actorNameEntity.Name);

        performedBy.ActorNameEntity = dialogSeenLogSeenByActor.ActorNameEntity;

        dialog.EndUserContext.UpdateSystemLabels(
            addLabels: [],
            removeLabels: [SystemLabel.Values.MarkedAsUnopened],
            performedBy);
        dialog.SeenLog.Add(seenLog);
        _db.DialogSeenLog.Add(seenLog);

        var result = await _unitOfWork
            .DisableUpdatableFilter()
            .DisableVersionableFilter()
            .SaveChangesAsync(cancellationToken);

        result.Switch(
            success => { },
            domainError =>
            {
                if (IsDuplicateActorNameError(domainError))
                {
                    throw new InvalidOperationException(domainError.Errors.First().ErrorMessage);
                }
                if (!IsDuplicateSeenLogIdError(domainError))
                {
                    throw new UnreachableException("Should not get domain error when updating SeenAt.");
                }
            },
            concurrencyError =>
                throw new UnreachableException("Should not get concurrencyError when updating SeenAt."),
            conflict => throw new UnreachableException("Should not get conflict when updating SeenAt."));
    }

    private static bool IsDuplicateSeenLogIdError(DomainError domainError) =>
        domainError.Errors.Any(x =>
            x.PropertyName == "DialogSeenLog"
         && x.ErrorMessage.Contains("(\'Id\')=", StringComparison.Ordinal)
         && x.ErrorMessage.Contains("already exists", StringComparison.Ordinal));

    private static bool IsDuplicateActorNameError(DomainError domainError) =>
        domainError.Errors.Any(x =>
            x.PropertyName == "ActorName"
         && x.ErrorMessage.Contains("(\'ActorId\', \'Name\')=", StringComparison.Ordinal)
         && x.ErrorMessage.Contains("already exists", StringComparison.Ordinal));
}
