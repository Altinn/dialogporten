using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

internal sealed class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator(IValidator<CreateActivityDto> createActivityValidator)
    {
        RuleFor(x => x.DialogId)
            .NotEmpty();

        RuleFor(x => x.Activities)
            .NotEmpty();

        RuleForEach(x => x.Activities)
            .SetValidator(createActivityValidator);
    }
}
