using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.Validators;

internal sealed class UpdateTransmissionTransmissionAttachmentUrlDtoValidator : AbstractValidator<TransmissionAttachmentUrlDto>
{
    public UpdateTransmissionTransmissionAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);

        RuleFor(x => x.MediaType)
            .MaximumLength(Constants.DefaultMediaTypeMaxLength);

        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}
