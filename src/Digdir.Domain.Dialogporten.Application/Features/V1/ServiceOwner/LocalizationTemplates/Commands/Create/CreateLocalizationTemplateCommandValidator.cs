using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Create;

internal sealed class CreateLocalizationTemplateCommandValidator : AbstractValidator<CreateLocalizationTemplateCommand>
{
    public CreateLocalizationTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateSet).SetValidator(new TemplateSetDtoValidator());
    }
}
