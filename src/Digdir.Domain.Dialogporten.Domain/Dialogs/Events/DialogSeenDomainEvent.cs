using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

public sealed record DialogSeenDomainEvent(
    Guid DialogId,
    string ServiceResource,
    string Party,
    string? Process,
    string? PrecedingProcess) : DomainEvent, IProcessEvent;
