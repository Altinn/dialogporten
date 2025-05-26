using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogTransmissionAttachmentUrlDtoValidator : AbstractValidator<TransmissionAttachmentUrlDto>
{
    public UpdateDialogTransmissionAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);

        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}
