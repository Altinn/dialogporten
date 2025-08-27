using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;

internal sealed class TemplateSetDtoValidator : AbstractValidator<LocalizationTemplateSetDto>
{
    public TemplateSetDtoValidator(int maximumLength = LocalizationValidatorConstants.MaximumLength)
    {
        RuleFor(x => x.Id).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Templates)
            .NotEmpty()
            .UniqueBy(x => x.LanguageCode)
            .ForEach(x => x.SetValidator(new TemplateDtoValidator(maximumLength)));
    }
}

internal sealed class TemplateDtoValidator : AbstractValidator<LocalizationTemplateDto>
{
    public TemplateDtoValidator(int maximumLength = LocalizationValidatorConstants.MaximumLength)
    {
        RuleFor(x => x).NotNull();

        RuleFor(x => x.Template)
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
