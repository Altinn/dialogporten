using AwesomeAssertions;
using AwesomeAssertions.Primitives;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

public static class DateTimeOffsetAssertionsExtensions
{
    /// <summary>
    /// Asserts the DateTimeOffset is within 1 microsecond of the expected value.
    /// </summary>
    public static AndConstraint<T> BeCloseToWithinMicrosecond<T>(
        this T should, DateTimeOffset expected)
        where T : DateTimeOffsetAssertions<T> =>
        should.BeCloseTo(expected, TimeSpan.FromMicroseconds(1));
}
