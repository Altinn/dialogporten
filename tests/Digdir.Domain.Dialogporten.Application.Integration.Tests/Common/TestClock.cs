using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class TestClock : IClock
{
    private DateTimeOffset? _override;

    public DateTimeOffset UtcNowOffset => _override ?? DateTimeOffset.UtcNow;
    public DateTimeOffset NowOffset => UtcNowOffset.ToOffset(TimeSpan.FromHours(1));

    public DateTime UtcNow => UtcNowOffset.UtcDateTime;
    public DateTime Now => NowOffset.DateTime;

    public void OverrideUtc(DateTimeOffset dateTimeOffset) => _override = dateTimeOffset;
    public void OverrideUtc(TimeSpan skew) => _override = UtcNowOffset.Add(skew);
    public void Reset() => _override = null;
}

internal static class TestClockExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep OverrideUtc(DateTimeOffset dateTimeOffset) => flowStep.Do(_ => DialogApplication.Clock.OverrideUtc(dateTimeOffset));
        public TFlowStep OverrideUtc(TimeSpan skew) => flowStep.Do(_ => DialogApplication.Clock.OverrideUtc(skew));
    }
}


