namespace Altinn.ApiClients.Dialogporten.Common;

internal interface IClock
{
    DateTimeOffset UtcNow { get; }
}

internal sealed class DefaultClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
