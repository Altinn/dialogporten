using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Update;

internal sealed class UpdateLocalizationTemplateCommandValidator : AbstractValidator<UpdateLocalizationTemplateCommand>
{
    public UpdateLocalizationTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateSet).SetValidator(new TemplateSetDtoValidator());
    }
}
