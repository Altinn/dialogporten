using AwesomeAssertions;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class DateTimeOffsetAssertionsExtensions
{
    extension(DateTimeOffset dateTime)
    {
        public void BeCloseToWithinSecond(DateTimeOffset expected) =>
            dateTime.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }
}
