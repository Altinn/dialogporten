using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

public sealed record DialogSeenDomainEvent(
    Guid DialogId,
    string ServiceResource,
    string Party,
    string? Process,
    string? PrecedingProcess,
    string UserId,
    DialogUserType.Values UserType,
    Guid? LastSeenId
    ) : DomainEvent, IProcessEvent;
