using Digdir.Domain.Dialogporten.Application.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class SkewedClock : IClock
{
    private readonly TimeSpan _skew;

    public SkewedClock(TimeSpan skew)
    {
        _skew = skew;
    }

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow.Add(_skew);
    public DateTimeOffset NowOffset => DateTimeOffset.Now.Add(_skew);

    public DateTime UtcNow => DateTime.UtcNow.Add(_skew);
    public DateTime Now => DateTime.Now.Add(_skew);
}
