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

        RuleFor(x => x.IsSilentUpdate)
            .Equal(true)
            .WithMessage("IsSilentUpdate=true query parameter is required to update transmissions.");

        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(transmissionValidator);

        RuleFor(x => x.Dto.RelatedTransmissionId)
            .NotEqual(x => x.TransmissionId)
            .WithMessage(x => $"A transmission cannot reference itself ({nameof(x.Dto.RelatedTransmissionId)} is equal to {nameof(x.TransmissionId)}, '{x.TransmissionId}').")
            .When(x => x.Dto?.RelatedTransmissionId.HasValue == true);
    }
}
