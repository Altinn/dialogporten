using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

public static class FluentValidationUriExtensions
{
    extension<T, TUri>(IRuleBuilder<T, TUri> ruleBuilder) where TUri : Uri?
    {
        public IRuleBuilderOptions<T, TUri> IsValidUri()
        {
            return ruleBuilder
                .Must(uri => uri is null || uri.IsWellFormedOriginalString())
                .WithMessage("'{PropertyName}' is not a well formatted URI.");
        }

        public IRuleBuilderOptions<T, TUri> MaximumLength(int maximumLength)
        {
            return ruleBuilder
                .Must((_, uri, context) =>
                {
                    var length = uri?.ToString().Length;
                    context.MessageFormatter
                        .AppendArgument("MaxLength", maximumLength)
                        .AppendArgument("TotalLength", length);
                    return !length.HasValue || length <= maximumLength;
                })
                .WithMessage("The length of '{PropertyName}' must be {MaxLength} characters or fewer. You entered {TotalLength} characters.");
        }
    }
}
