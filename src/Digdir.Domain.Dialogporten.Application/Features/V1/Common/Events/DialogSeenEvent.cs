using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserIdType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;

public sealed class DialogSeenEvent(
    IPartyNameRegistry partyNameRegistry,
    IDialogDbContext db
) : INotificationHandler<DialogSeenDomainEvent>
{
    private readonly IPartyNameRegistry _partyNameRegistry = partyNameRegistry ?? throw new ArgumentNullException(nameof(partyNameRegistry));
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task Handle(DialogSeenDomainEvent dialogSeenDomainEvent, CancellationToken cancellationToken)
    {
        var userTypeId = dialogSeenDomainEvent.UserType;
        var endUserId = dialogSeenDomainEvent.EnduserId;
        var userId = new UserId
        {
            Type = userTypeId,
            ExternalId = endUserId,
        };

        var name = userTypeId switch
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

        var dialog = _db.Dialogs
            .Where(x => x.Id == dialogSeenDomainEvent.DialogId)
            .Include(x => x.SeenLog
                .Where(log => log.CreatedAt >= log.Dialog.ContentUpdatedAt)
                .OrderBy(log => log.CreatedAt))
            .FirstOrDefault();

        if (dialog == null)
        {
            return;
        }

        var lastSeenAt = dialog.SeenLog
                .Where(x => x.SeenBy.ActorNameEntity?.ActorId == endUserId)
                .MaxBy(x => x.CreatedAt)
                ?.CreatedAt
         ?? DateTimeOffset.MinValue;

        if (lastSeenAt >= dialog.UpdatedAt)
        {
            return;
        }

        dialog.SeenLog.Add(new DialogSeenLog
        {
            EndUserTypeId = userTypeId,
            IsViaServiceOwner = userTypeId == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson,
            LastSeenLogId = dialogSeenDomainEvent.SeenLogId + 1,
            SeenBy = new DialogSeenLogSeenByActor
            {
                ActorTypeId = ActorType.Values.PartyRepresentative,
                ActorNameEntity = new ActorName
                {
                    Name = name,
                    ActorId = endUserId
                }
            }
        });
    }
}
