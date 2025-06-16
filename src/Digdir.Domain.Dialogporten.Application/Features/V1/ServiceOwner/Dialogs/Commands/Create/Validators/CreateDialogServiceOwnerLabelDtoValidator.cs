using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogServiceOwnerLabelDtoValidator : AbstractValidator<ServiceOwnerLabelDto>
{
    public CreateDialogServiceOwnerLabelDtoValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .MinimumLength(Constants.MinSearchStringLength)
            .MaximumLength(Constants.DefaultMaxStringLength);
    }
}
