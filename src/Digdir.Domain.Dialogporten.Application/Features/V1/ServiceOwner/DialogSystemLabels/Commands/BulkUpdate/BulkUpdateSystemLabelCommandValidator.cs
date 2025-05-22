using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkUpdate;

public sealed class BulkUpdateSystemLabelCommandValidator : AbstractValidator<BulkUpdateSystemLabelCommand>
{
    private const int MaxDialogIdsPerRequest = 100;
    public BulkUpdateSystemLabelCommandValidator()
    {
        RuleFor(x => x.DialogIds)
            .NotNull()
            .NotEmpty()
            .Must(x => x.Count <= MaxDialogIdsPerRequest)
            .WithMessage($"A maximum of {MaxDialogIdsPerRequest} dialog ids are allowed");

        RuleFor(x => x.Labels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is supported");
    }
}
