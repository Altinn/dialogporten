using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

public sealed record DialogRestoredDomainEvent(
    Guid DialogId,
    string ServiceResource,
    string Party,
    string? Process,
    string? PrecedingProcess) : DomainEvent, IProcessEvent;
