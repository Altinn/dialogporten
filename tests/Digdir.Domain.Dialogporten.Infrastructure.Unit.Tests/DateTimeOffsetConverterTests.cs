using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.ValueConverters;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class DateTimeOffsetConverterTests
{
    private static readonly DateTimeOffsetConverter Converter = new();

    private static DateTimeOffset ToProvider(DateTimeOffset value) =>
        (DateTimeOffset)Converter.ConvertToProvider(value)!;

    [Fact]
    public void MaxValue_Is_Nudged_Below_Infinity()
    {
        var result = ToProvider(DateTimeOffset.MaxValue);

        result.Should().NotBe(DateTimeOffset.MaxValue);
        result.Should().Be(DateTimeOffset.MaxValue.ToUniversalTime().AddSeconds(-1));
    }

    [Fact]
    public void MinValue_Is_Nudged_Above_NegativeInfinity()
    {
        var result = ToProvider(DateTimeOffset.MinValue);

        result.Should().NotBe(DateTimeOffset.MinValue);
        result.Should().Be(DateTimeOffset.MinValue.ToUniversalTime().AddSeconds(1));
    }

    [Fact]
    public void Normal_Value_Is_Preserved_As_Utc()
    {
        var value = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

        var result = ToProvider(value);

        result.Should().Be(value.ToUniversalTime());
        result.Offset.Should().Be(TimeSpan.Zero);
    }
}
