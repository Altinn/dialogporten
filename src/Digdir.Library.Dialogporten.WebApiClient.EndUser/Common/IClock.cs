namespace Altinn.ApiClients.Dialogporten.EndUser.Common;

internal interface IClock
{
    DateTimeOffset UtcNow { get; }
}

internal class DefaultClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
