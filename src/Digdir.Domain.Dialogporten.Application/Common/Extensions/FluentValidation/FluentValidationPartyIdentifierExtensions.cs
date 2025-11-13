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
                // Disabled for now, as we do not fully support these user types in Altinn yet
                //or IdportenSelfIdentifiedUserIdentifier
                //or FeideUserIdentifier
                ))
            .WithMessage(
                $"'{{PropertyName}}' must be on format '{NorwegianOrganizationIdentifier.PrefixWithSeparator}{{norwegian org-nr}}', " +
                $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}', or " +
                $"'{AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator}{{username}}'" +

                // Disabled for now, as we do not fully support these user types in Altinn yet
                //$"'{IdportenSelfIdentifiedUserIdentifier.PrefixWithSeparator}{{e-mail}}' or " +
                //$"'{FeideUserIdentifier.PrefixWithSeparator}{{subject}}' " +
                "with valid values, respectively.");
    }
}
