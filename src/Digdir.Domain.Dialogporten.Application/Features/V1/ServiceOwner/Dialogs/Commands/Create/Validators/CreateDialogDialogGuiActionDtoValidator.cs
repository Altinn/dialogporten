#pragma warning disable CS0618 // Obsolete legacy authorization fields are validated for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Http;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogGuiActionDtoValidator : AbstractValidator<GuiActionDto>
{
    public CreateDialogDialogGuiActionDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<AuthorizationContextDto> authorizationContextValidator,
        IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage($"'{{PropertyName}}' must not be empty when '{nameof(GuiActionDto.AuthorizationContext)}' is not supplied.")
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.AuthorizationContext is null);

        RuleFor(x => x.Action)
            .Null()
            .WithMessage($"'{{PropertyName}}' cannot be combined with '{nameof(GuiActionDto.AuthorizationContext)}'; use '{nameof(GuiActionDto.AuthorizationContext)}.{nameof(AuthorizationContextDto.Action)}' instead.")
            .When(x => x.AuthorizationContext is not null);

        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);

        RuleFor(x => x.AuthorizationAttribute)
            .IsValidAuthorizationAttribute();

        RuleFor(x => x.AuthorizationAttribute)
            .Null()
            .WithMessage($"'{{PropertyName}}' cannot be combined with '{nameof(GuiActionDto.AuthorizationContext)}'.")
            .When(x => x.AuthorizationContext is not null);

        When(x => x.AuthorizationContext is not null, () =>
        {
            RuleFor(x => x.AuthorizationContext!.Action)
                .NotEmpty()
                .OverridePropertyName($"{nameof(GuiActionDto.AuthorizationContext)}.{nameof(AuthorizationContextDto.Action)}")
                .WithMessage("'{PropertyName}' is required on GUI actions.");

            RuleFor(x => x.AuthorizationContext)
                .SetValidator(authorizationContextValidator!);
        });

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.HttpMethod)
            .Must(x => x is HttpVerb.Values.GET or HttpVerb.Values.POST or HttpVerb.Values.DELETE)
            .WithMessage($"'{{PropertyName}}' for GUI actions must be one of the following: " +
                         $"[{HttpVerb.Values.GET}, {HttpVerb.Values.POST}, {HttpVerb.Values.DELETE}].");

        RuleFor(x => x.Title)
            .NotEmpty()
            .SetValidator(localizationsValidator);

        RuleFor(x => x.Prompt)
            .SetValidator(localizationsValidator!)
            .When(x => x.Prompt != null);
    }
}
