using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogTransmissionNavigationalActionDtoValidator : AbstractValidator<TransmissionNavigationalActionDto>
{
    public UpdateDialogTransmissionNavigationalActionDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IClock clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .SetValidator(localizationsValidator);

        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);

        RuleFor(x => x.ExpiresAt)
            .IsInFuture(clock);
    }
}
