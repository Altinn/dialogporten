using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Extensions.FluentValidation;

public class FluentValidationDateTimeOffsetExtensionsTests
{
    private sealed record Model(DateTimeOffset Required, DateTimeOffset? Optional);

    private sealed class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            RuleFor(x => x.Required).IsNotMaxValue();
            RuleFor(x => x.Optional).IsNotMaxValue();
        }
    }

    [Fact]
    public void IsNotMaxValue_Allows_Null_Optional()
    {
        var result = new ModelValidator().Validate(new Model(DateTimeOffset.MinValue, Optional: null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsNotMaxValue_Allows_MinValue_Sentinel_And_Normal_Dates()
    {
        // DateTimeOffset.MinValue / default is the "not supplied, use current time" sentinel
        // (handled by the ICreatableEntity hook) and must remain valid.
        var normal = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        new ModelValidator().Validate(new Model(DateTimeOffset.MinValue, DateTimeOffset.MinValue))
            .IsValid.Should().BeTrue();
        new ModelValidator().Validate(new Model(normal, normal))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsNotMaxValue_Rejects_MaxValue()
    {
        var result = new ModelValidator().Validate(new Model(DateTimeOffset.MaxValue, DateTimeOffset.MaxValue));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Model.Required));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Model.Optional));
    }
}
