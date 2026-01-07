using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

internal sealed class CreateTransmissionCommandValidator : AbstractValidator<CreateTransmissionCommand>
{
    public CreateTransmissionCommandValidator(IValidator<CreateTransmissionDto> transmissionValidator)
    {
        RuleFor(x => x.DialogId)
            .NotEmpty();

        RuleFor(x => x.Transmissions)
            .NotEmpty();

        RuleForEach(x => x.Transmissions)
            .SetValidator(transmissionValidator);
    }
}
