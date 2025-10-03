namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }
    DateTime UtcNow { get; }
    DateTime Now { get; }
}

internal sealed class Clock : IClock
{
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
    public DateTimeOffset NowOffset => DateTimeOffset.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}
