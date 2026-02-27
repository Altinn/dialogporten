using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;

public sealed class DialogSeenEvent(
    IDialogDbContext db,
    IUnitOfWork unitOfWork
) : INotificationHandler<DialogSeenDomainEvent>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task Handle(DialogSeenDomainEvent dialogSeenDomainEvent, CancellationToken cancellationToken)
    {
        var dialog = await _db.Dialogs
            .Include(x => x.SeenLog
                .Where(log => log.Id == dialogSeenDomainEvent.SeenLogId))
            .Include(x => x.EndUserContext)
                .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == dialogSeenDomainEvent.DialogId, cancellationToken);

        if (dialog is null)
        {
            return;
        }

        // Short circuit if LastSeen already exists for the new Id.
        if (dialog.SeenLog.Count != 0)
        {
            return;
        }

        var actorNameEntity = new ActorName { ActorId = dialogSeenDomainEvent.UserId };

        var dialogSeenLogSeenByActor = new DialogSeenLogSeenByActor
        {
            ActorTypeId = ActorType.Values.PartyRepresentative,
            ActorNameEntity = actorNameEntity
        };

        var seenLog = new DialogSeenLog
        {
            Id = dialogSeenDomainEvent.SeenLogId,
            EndUserTypeId = dialogSeenDomainEvent.UserType,
            IsViaServiceOwner = dialogSeenDomainEvent.UserType == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson,
            SeenBy = dialogSeenLogSeenByActor,
            CreatedAt = dialogSeenDomainEvent.OccurredAt
        };

        var performedBy = new LabelAssignmentLogActor
        {
            ActorTypeId = dialogSeenLogSeenByActor.ActorTypeId,
            ActorNameEntity = actorNameEntity
        };

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
