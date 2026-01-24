using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentValidation;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.ValidationErrorStrings;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;

internal sealed class BulkSetSystemLabelCommandValidator : AbstractValidator<BulkSetSystemLabelCommand>
{
    public BulkSetSystemLabelCommandValidator(IValidator<BulkSetSystemLabelDto> dtoValidator)
    {
        When(x => x.Dto?.PerformedBy is null, () =>
        {
            RuleFor(x => x.EndUserId)
                .NotEmpty()
                .WithMessage("EnduserId is required");
        });

        When(x => x.Dto?.PerformedBy is not null, () =>
        {
            RuleFor(x => x.EndUserId)
                .Empty()
                .WithMessage("EnduserId must be omitted when performedBy is supplied.");
        });

        RuleFor(x => x.Dto)
            .NotNull()
            .SetValidator(dtoValidator);
    }
}

internal sealed class BulkSetSystemLabelDtoValidator : AbstractValidator<BulkSetSystemLabelDto>
{
    private const int MaxDialogsPerRequest = 200;

    public BulkSetSystemLabelDtoValidator(IValidator<ActorDto> actorValidator)
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

        When(x => x.PerformedBy is not null, () =>
        {
            RuleFor(x => x.PerformedBy!)
                .SetValidator(actorValidator);
        });
    }
}
