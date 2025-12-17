using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

public static class FluentValidationPartyIdentifierExtensions
{
    public static IRuleBuilderOptions<T, string> IsValidPartyIdentifier<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(identifier => identifier is null
                || (
                    PartyIdentifier.TryParse(identifier, out var id)
                    && id
                        is NorwegianPersonIdentifier
                        or NorwegianOrganizationIdentifier
                        or AltinnSelfIdentifiedUserIdentifier
                        or IdportenEmailUserIdentifier
                // Disabled for now, as this user type is not yet addressable
                //or FeideUserIdentifier
                ))
            .WithMessage(
                $"'{{PropertyName}}' must be on format '{NorwegianOrganizationIdentifier.PrefixWithSeparator}{{norwegian org-nr}}', " +
                $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}', " +
                $"'{AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator}{{username}}' or " +
                $"'{IdportenEmailUserIdentifier.PrefixWithSeparator}{{e-mail}}' " +
                // Disabled for now, as this user type is not yet addressable
                //$"'{FeideUserIdentifier.PrefixWithSeparator}{{subject}}' " +
                "with valid values, respectively.");
    }
}
