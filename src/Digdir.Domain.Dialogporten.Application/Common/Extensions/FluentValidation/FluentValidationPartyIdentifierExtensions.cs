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
                    && id is NorwegianPersonIdentifier or NorwegianOrganizationIdentifier or GenericPartyIdentifier
                ))
            .WithMessage(
                $"'{{PropertyName}}' must be in the format '{NorwegianOrganizationIdentifier.PrefixWithSeparator}{{norwegian org-nr}}', " +
                $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}' or " +
                $"'{GenericPartyIdentifier.PrefixWithSeparator}{{uuid}}' with valid values.");
    }
}
