using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetSystemLabelCommandValidator : AbstractValidator<BulkSetSystemLabelCommand>
{
    private const int MaxDialogsPerRequest = 200;

    public BulkSetSystemLabelCommandValidator()
    {
        RuleFor(x => x.EndUserId)
            .NotEmpty()
            .WithMessage("EnduserId is required");

        RuleFor(x => x.Dto.Dialogs)
            .NotNull()
            .Must(x => x.Count is > 0 and <= MaxDialogsPerRequest)
            .WithMessage($"Must supply between 1 and {MaxDialogsPerRequest} dialogs to update")
            .UniqueBy(x => x.DialogId);

        RuleFor(x => x.Dto.SystemLabels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is currently supported");
    }
}
