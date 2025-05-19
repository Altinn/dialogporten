using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

internal static class LocalizationValidatorConstants
{
    public const int MaximumLength = 255;

    public const string NormalizationErrorMessage =
        "Culture specific codes like 'en_GB' and 'en-US' are normalized to 'en' (ISO 639).";

    public const string InvalidCultureCodeErrorMessageWithNorwegianHint =
        InvalidCultureCodeErrorMessage + "Use 'nb' or 'nn' for Norwegian. ";

    public const string InvalidCultureCodeErrorMessage =
        "'{PropertyName}' '{PropertyValue}' is not a valid language code. ";
}

internal sealed class LocalizationDtosValidator : AbstractValidator<IEnumerable<LocalizationDto>>
{
    public LocalizationDtosValidator(int maximumLength = LocalizationValidatorConstants.MaximumLength)
    {
        RuleFor(x => x)
            .UniqueBy(x => x.LanguageCode)
            .WithMessage(localizations =>
            {
                var duplicates = localizations
                    .GroupBy(y => y.LanguageCode)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                return $"Can not contain duplicate items: [{string.Join(", ", duplicates)}]. " +
                       $"{LocalizationValidatorConstants.NormalizationErrorMessage}";
            })
            .ForEach(x => x.SetValidator(new LocalizationDtoValidator(maximumLength)));
    }
}

internal sealed class LocalizationDtoValidator : AbstractValidator<LocalizationDto>
{
    public LocalizationDtoValidator(int maximumLength = LocalizationValidatorConstants.MaximumLength)
    {
        RuleFor(x => x)
            .NotNull();

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(maximumLength);

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(Localization.IsValidCultureCode)
            .WithMessage(localization =>
                (localization.LanguageCode == "no"
                    ? LocalizationValidatorConstants.InvalidCultureCodeErrorMessageWithNorwegianHint
                    : LocalizationValidatorConstants.InvalidCultureCodeErrorMessage) +
                LocalizationValidatorConstants.NormalizationErrorMessage);
    }
}
