using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentValidation;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.ValidationErrorStrings;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;

internal sealed class SetSystemLabelCommandValidator : AbstractValidator<SetSystemLabelCommand>
{
    public SetSystemLabelCommandValidator(IValidator<ActorDto> actorValidator)
    {
        When(x => x.PerformedBy is null, () =>
        {
            RuleFor(x => x.EndUserId)
                .NotEmpty()
                .WithMessage("EnduserId is required");
        });

        When(x => x.PerformedBy is not null, () =>
        {
            RuleFor(x => x.EndUserId)
                .Empty()
                .WithMessage("EnduserId must be omitted when performedBy is supplied.");

            RuleFor(x => x.PerformedBy!)
                .SetValidator(actorValidator);
        });

        RuleForEach(x => x.AddLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);

        RuleForEach(x => x.RemoveLabels)
            .Must(label => label != SystemLabel.Values.Sent)
            .WithMessage(SentLabelNotAllowed);
    }
}
