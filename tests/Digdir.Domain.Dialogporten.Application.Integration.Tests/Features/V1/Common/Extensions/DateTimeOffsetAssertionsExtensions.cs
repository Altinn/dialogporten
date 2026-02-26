using AwesomeAssertions;
using AwesomeAssertions.Primitives;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

public static class DateTimeOffsetAssertionsExtensions
{
    public static AndConstraint<T> BeCloseToDefault<T>(
        this T should, DateTimeOffset expected)
        where T : DateTimeOffsetAssertions<T> =>
        should.BeCloseTo(expected, TimeSpan.FromSeconds(10));
}
