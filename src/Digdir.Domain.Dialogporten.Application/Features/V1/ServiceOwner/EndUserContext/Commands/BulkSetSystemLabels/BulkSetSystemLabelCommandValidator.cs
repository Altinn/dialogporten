using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentValidation;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.ValidationErrorStrings;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

internal sealed class BulkSetSystemLabelCommandValidator : AbstractValidator<BulkSetSystemLabelCommand>
{
    public BulkSetSystemLabelCommandValidator(IValidator<BulkSetSystemLabelDto> dtoValidator)
    {
        RuleFor(x => x.EndUserId)
            .NotEmpty()
            .WithMessage("EnduserId is required");

        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(dtoValidator);
    }
}

internal sealed class BulkSetSystemLabelDtoValidator : AbstractValidator<BulkSetSystemLabelDto>
{
    private const int MaxDialogsPerRequest = 200;

    public BulkSetSystemLabelDtoValidator()
    {
        RuleFor(x => x.Dialogs)
            .NotNull()
            .Must(x => x.Count is > 0 and <= MaxDialogsPerRequest)
            .WithMessage($"Must supply between 1 and {MaxDialogsPerRequest} dialogs to update")
            .UniqueBy(x => x.DialogId);

        RuleForEach(x => x.AddLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);

        RuleForEach(x => x.RemoveLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);
    }
}
