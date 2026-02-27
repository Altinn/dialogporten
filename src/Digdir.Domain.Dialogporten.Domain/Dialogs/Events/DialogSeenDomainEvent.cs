using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

public sealed record DialogSeenDomainEvent : DomainEvent, IProcessEvent
{
    public Guid DialogId { get; }
    public string ServiceResource { get; }
    public string Party { get; }
    public string? Process { get; }
    public string? PrecedingProcess { get; }
    public string UserId { get; }
    public DialogUserType.Values UserType { get; }
    public Guid SeenLogId { get; }

    public DialogSeenDomainEvent(Guid DialogId,
        string ServiceResource,
        string Party,
        string? Process,
        string? PrecedingProcess,
        string UserId,
        DialogUserType.Values UserType,
        Guid SeenLogId)
    {
        this.DialogId = DialogId;
        this.ServiceResource = ServiceResource;
        this.Party = Party;
        this.Process = Process;
        this.PrecedingProcess = PrecedingProcess;
        this.UserId = UserId;
        this.UserType = UserType;
        this.SeenLogId = SeenLogId;
        EventId = SeenLogId;
    }
}
