#pragma warning disable CS0618 // Obsolete legacy authorization fields are validated for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDialogApiActionDtoValidator : AbstractValidator<ApiActionDto>
{
    public UpdateDialogDialogApiActionDtoValidator(
        IValidator<ApiActionEndpointDto> apiActionEndpointValidator,
        IValidator<AuthorizationContextDto> authorizationContextValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage($"'{{PropertyName}}' must not be empty when '{nameof(ApiActionDto.AuthorizationContext)}' is not supplied.")
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.AuthorizationContext is null);

        RuleFor(x => x.Action)
            .Null()
            .WithMessage($"'{{PropertyName}}' cannot be combined with '{nameof(ApiActionDto.AuthorizationContext)}'; use '{nameof(ApiActionDto.AuthorizationContext)}.{nameof(AuthorizationContextDto.Action)}' instead.")
            .When(x => x.AuthorizationContext is not null);

        RuleFor(x => x.AuthorizationAttribute)
            .IsValidAuthorizationAttribute();

        RuleFor(x => x.AuthorizationAttribute)
            .Null()
            .WithMessage($"'{{PropertyName}}' cannot be combined with '{nameof(ApiActionDto.AuthorizationContext)}'.")
            .When(x => x.AuthorizationContext is not null);

        When(x => x.AuthorizationContext is not null, () =>
        {
            RuleFor(x => x.AuthorizationContext!.Action)
                .NotEmpty()
                .OverridePropertyName($"{nameof(ApiActionDto.AuthorizationContext)}.{nameof(AuthorizationContextDto.Action)}")
                .WithMessage("'{PropertyName}' is required on API actions.");

            RuleFor(x => x.AuthorizationContext)
                .SetValidator(authorizationContextValidator!);
        });

        RuleFor(x => x.Name)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Endpoints)
            .UniqueBy(x => x.Id);

        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}
