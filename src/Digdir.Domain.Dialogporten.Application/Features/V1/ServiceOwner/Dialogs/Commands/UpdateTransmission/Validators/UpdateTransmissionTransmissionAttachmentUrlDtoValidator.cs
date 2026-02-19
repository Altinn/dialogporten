using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.Validators;

internal sealed class UpdateTransmissionTransmissionAttachmentUrlDtoValidator : AbstractValidator<TransmissionAttachmentUrlDto>
{
    public UpdateTransmissionTransmissionAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .IsValidUri();

        RuleFor(x => x.MediaType)
            .MaximumLength(256);

        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}
