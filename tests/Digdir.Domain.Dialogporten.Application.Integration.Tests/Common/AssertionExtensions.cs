using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public static class ValidationErrorAssertionsExtensions
{
    public static void ShouldHaveErrorWithText(this ValidationError validationError, string expectedText) =>
        validationError.Errors.Should().Contain(
            e => e.ErrorMessage.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            $"Expected error containing the text '{expectedText}'");
}

public static class DomainErrorAssertionsExtensions
{
    extension(DomainError domainError)
    {
        public void ShouldHaveErrorWithText(string expectedText) =>
            domainError.Errors.Should().Contain(
                e => e.ErrorMessage.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
                $"Expected an error containing the text '{expectedText}'");

        public void ShouldHaveErrorWithPropertyNameText(string expectedText) =>
            domainError.Errors.Should().Contain(
                e => e.PropertyName.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
                $"Expected an error with {nameof(DomainFailure.PropertyName)} " +
                $"containing the text '{expectedText}'");
    }
}
