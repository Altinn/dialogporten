#pragma warning disable CS0618 // Obsolete legacy authorization fields are validated for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogApiActionDtoValidator : AbstractValidator<ApiActionDto>
{
    public CreateDialogDialogApiActionDtoValidator(
        IValidator<ApiActionEndpointDto> apiActionEndpointValidator,
        IValidator<AuthorizationContextDto> authorizationContextValidator,
        IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

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

        RuleFor(x => x.AuthorizationContext)
            .SetValidator(authorizationContextValidator!)
            .When(x => x.AuthorizationContext is not null);

        RuleFor(x => x.Name)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}
