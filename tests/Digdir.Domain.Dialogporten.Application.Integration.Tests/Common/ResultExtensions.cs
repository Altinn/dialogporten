using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public static class ValidationErrorAssertionsExtensions
{
    public static void ShouldHaveErrorWithText(this ValidationError validationError, string expectedText)
    {
        validationError.Errors.Should().Contain(
            e => e.ErrorMessage.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            $"Expected error containing the text '{expectedText}'");
    }
}

public static class DomainErrorAssertionsExtensions
{
    public static void ShouldHaveErrorWithText(this DomainError domainError, string expectedText)
    {
        domainError.Errors.Should().Contain(
            e => e.ErrorMessage.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            $"Expected an error containing the text '{expectedText}'");
    }
}
