using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;

public sealed class BulkSetSystemLabelCommandValidator : AbstractValidator<BulkSetSystemLabelCommand>
{
    private const int MaxDialogIdsPerRequest = 100;

    public BulkSetSystemLabelCommandValidator()
    {
        RuleFor(x => x.EnduserId)
            .NotEmpty()
            .WithMessage("EnduserId is required");

        RuleFor(x => x.DialogIds)
            .NotNull()
            .Must(x => x.Count is > 0 and <= MaxDialogIdsPerRequest)
            .WithMessage($"DialogIds must be between 1 and {MaxDialogIdsPerRequest}");

        RuleFor(x => x.SystemLabels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is supported");
    }
}
