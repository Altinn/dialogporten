namespace Digdir.Domain.Dialogporten.Infrastructure.GraphQL;

internal struct DialogEventPayload
{
    public Guid Id { get; set; }
    public DialogEventType Type { get; set; }
}

internal enum DialogEventType
{
    DialogUpdated = 1,
    DialogDeleted = 2
}
