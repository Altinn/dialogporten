using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.DialogSystemLabels.Commands.BulkSet;

internal sealed class BulkSetSystemLabelCommandValidator : AbstractValidator<BulkSetSystemLabelCommand>
{
    private const int MaxDialogsPerRequest = 100;

    public BulkSetSystemLabelCommandValidator(IValidator<BulkSetSystemLabelDto> validator)
    {
        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(validator);
    }
}

internal sealed class BulkSetSystemLabelDtoValidator : AbstractValidator<BulkSetSystemLabelDto>
{
    private const int MaxDialogsPerRequest = 100;

    public BulkSetSystemLabelDtoValidator()
    {
        RuleFor(x => x.Dialogs)
            .NotNull()
            .Must(x => x.Count is > 0 and <= MaxDialogsPerRequest)
            .WithMessage($"Must supply between 1 and {MaxDialogsPerRequest} dialogs to update")
            .UniqueBy(x => x.DialogId);

        RuleFor(x => x.SystemLabels)
            .NotNull()
            .Must(x => x.Count <= 1)
            .WithMessage("Only one system label is currently supported");
    }
}
