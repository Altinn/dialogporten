using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentValidation;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.ValidationErrorStrings;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;

internal sealed class SetSystemLabelCommandValidator : AbstractValidator<SetSystemLabelCommand>
{
    public SetSystemLabelCommandValidator()
    {
        RuleFor(x => x.EndUserId)
            .NotEmpty()
            .WithMessage("EnduserId is required");

        RuleForEach(x => x.AddLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);

        RuleForEach(x => x.RemoveLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);
    }
}
