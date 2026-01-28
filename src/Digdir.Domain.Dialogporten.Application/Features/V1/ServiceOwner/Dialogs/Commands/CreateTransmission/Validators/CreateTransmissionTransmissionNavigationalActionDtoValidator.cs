using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission.Validators;

internal sealed class CreateTransmissionTransmissionNavigationalActionDtoValidator : AbstractValidator<TransmissionNavigationalActionDto>
{
    public CreateTransmissionTransmissionNavigationalActionDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .SetValidator(localizationsValidator);

        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);
    }
}
