using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDialogApiActionDtoValidator : AbstractValidator<ApiActionDto>
{
    public UpdateDialogDialogApiActionDtoValidator(
        IValidator<ApiActionEndpointDto> apiActionEndpointValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Name)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Endpoints)
            .UniqueBy(x => x.Id);

        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}
