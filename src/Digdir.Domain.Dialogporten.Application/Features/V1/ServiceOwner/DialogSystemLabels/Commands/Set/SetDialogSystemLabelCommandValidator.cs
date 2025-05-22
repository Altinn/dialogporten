using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.Set;

public sealed class SetDialogSystemLabelCommandValidator : AbstractValidator<SetSystemLabelCommand>
{
    public SetDialogSystemLabelCommandValidator()
    {
        RuleFor(x => x.EnduserId)
            .NotEmpty()
            .WithMessage("EnduserId is required");

        RuleFor(x => x.SystemLabels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is supported");
    }
}
