using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission.Validators;

internal sealed class CreateTransmissionTransmissionAttachmentUrlDtoValidator : AbstractValidator<TransmissionAttachmentUrlDto>
{
    public CreateTransmissionTransmissionAttachmentUrlDtoValidator()
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
