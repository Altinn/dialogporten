namespace Altinn.ApiClients.Dialogporten.Common;

internal interface IClock
{
    DateTimeOffset UtcNow { get; }
}

internal class DefaultClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
