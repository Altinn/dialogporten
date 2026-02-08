using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

internal sealed class UpdateTransmissionCommandValidator : AbstractValidator<UpdateTransmissionCommand>
{
    public UpdateTransmissionCommandValidator(IValidator<UpdateTransmissionDto> transmissionValidator)
    {
        RuleFor(x => x.DialogId)
            .NotEmpty();

        RuleFor(x => x.TransmissionId)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(transmissionValidator);

        RuleFor(x => x.IsSilentUpdate)
            .Equal(true)
            .WithMessage($"{nameof(UpdateTransmissionCommand.IsSilentUpdate)} must be true when updating transmissions.");
    }
}
