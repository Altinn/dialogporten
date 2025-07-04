using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;

public sealed class SetSystemLabelCommandValidator : AbstractValidator<SetSystemLabelCommand>
{
    public SetSystemLabelCommandValidator()
    {
        RuleFor(x => x.SystemLabels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is currently supported");
    }
}
