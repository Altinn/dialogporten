using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

internal sealed class AuthorizationContextDtoValidator : AbstractValidator<AuthorizationContextDto>
{
    public AuthorizationContextDtoValidator()
    {
        RuleFor(x => x.ServiceResource)
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' must start with '{Constants.ServiceResourcePrefix}'.");

        RuleFor(x => x.AdditionalResourceAttribute)
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || !x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' cannot contain a service resource reference ('{Constants.ServiceResourcePrefix}...'); use 'ServiceResource' instead.");

        RuleFor(x => x.Parties)
            .Must(x => x.Count <= AuthorizationContext.MaxNumberOfParties)
            .WithMessage($"'{{PropertyName}}' cannot contain more than {AuthorizationContext.MaxNumberOfParties} parties.");

        RuleFor(x => x.Parties)
            .UniqueBy(x => x);

        RuleFor(x => x.Parties)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must contain at least one party when 'IncludeDialogParty' is false.")
            .When(x => !x.IncludeDialogParty);

        RuleForEach(x => x.Parties)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .IsValidPartyIdentifier();

        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.Action is not null);

        RuleFor(x => x.UnauthorizedPresentation)
            .IsInEnum()
            .WithMessage($"'{{PropertyName}}' is required and must be either " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Disabled}' or " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Excluded}'.");
    }
}
