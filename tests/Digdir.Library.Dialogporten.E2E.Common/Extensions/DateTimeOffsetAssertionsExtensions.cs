using AwesomeAssertions;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class DateTimeOffsetAssertionsExtensions
{
    public static AndConstraint<T> BeCloseToWithinSecond<T>(
        this T should, DateTimeOffset expected)
        where T : DateTimeOffsetAssertions<T> =>
        should.BeCloseTo(expected, TimeSpan.FromSeconds(1));
}
