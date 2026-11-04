using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Extensions.FluentValidation;

public class FluentValidationDateTimeOffsetExtensionsTests
{
    private sealed record Model(DateTimeOffset? CreatedAt);

    private sealed class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            RuleFor(x => x.CreatedAt).IsFinite();
        }
    }

    [Fact]
    public void IsFinite_Allows_Null_And_Normal_Dates()
    {
        // null is "not supplied" and must stay valid.
        new ModelValidator().Validate(new Model(null)).IsValid.Should().BeTrue();

        var normal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        new ModelValidator().Validate(new Model(normal)).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InfinitySentinels))]
    public void IsFinite_Rejects_Infinity_Sentinels(DateTimeOffset value)
    {
        var result = new ModelValidator().Validate(new Model(value));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Model.CreatedAt));
    }

    public static TheoryData<DateTimeOffset> InfinitySentinels() =>
    [
        DateTimeOffset.MinValue, // -> PostgreSQL '-infinity'
        DateTimeOffset.MaxValue, // -> PostgreSQL 'infinity'
    ];
}
