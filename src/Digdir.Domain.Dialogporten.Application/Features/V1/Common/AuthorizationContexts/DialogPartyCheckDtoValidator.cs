using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

internal sealed class DialogPartyCheckDtoValidator : AbstractValidator<DialogPartyCheckDto>
{
    public DialogPartyCheckDtoValidator()
    {
        RuleFor(x => x.AccessRestriction)
            .IsInEnum()
            .WithMessage($"'{{PropertyName}}' is required and must be one of " +
                         $"'{DialogPartyCheckAccessRestriction.Values.None}', " +
                         $"'{DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy}' or " +
                         $"'{DialogPartyCheckAccessRestriction.Values.ClientDelegation}'.");

        RuleFor(x => x.ViaParty)
            .NotEmpty()
            .WithMessage($"'{{PropertyName}}' is required when '{nameof(DialogPartyCheckDto.AccessRestriction)}' is " +
                         $"'{DialogPartyCheckAccessRestriction.Values.ClientDelegation}'.")
            .MaximumLength(Constants.DefaultMaxStringLength)
            .Must(x => x is null || (PartyIdentifier.TryParse(x, out var id) && id is NorwegianOrganizationIdentifier))
            .WithMessage($"'{{PropertyName}}' must be on format '{NorwegianOrganizationIdentifier.PrefixWithSeparator}{{norwegian org-nr}}' " +
                         "with a valid organization number.")
            .When(x => x.AccessRestriction is DialogPartyCheckAccessRestriction.Values.ClientDelegation);

        RuleFor(x => x.ViaParty)
            .Null()
            .WithMessage($"'{{PropertyName}}' is only allowed when '{nameof(DialogPartyCheckDto.AccessRestriction)}' is " +
                         $"'{DialogPartyCheckAccessRestriction.Values.ClientDelegation}'.")
            .When(x => x.AccessRestriction is not DialogPartyCheckAccessRestriction.Values.ClientDelegation);

        RuleFor(x => x.ServiceResource)
            .IsValidContextServiceResource();

        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.Action is not null);
    }
}

internal static class AuthorizationContextValidationExtensions
{
    /// <summary>
    /// The rules for a service resource referenced by an authorization context or one of its dialog party checks.
    /// Ownership of the resource is verified separately, by the service resource authorizer.
    /// </summary>
    internal static IRuleBuilderOptions<T, string?> IsValidContextServiceResource<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' must start with '{Constants.ServiceResourcePrefix}'.");
}
