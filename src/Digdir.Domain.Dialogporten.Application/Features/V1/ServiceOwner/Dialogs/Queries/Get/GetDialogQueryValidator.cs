using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal sealed class GetDialogQueryValidator : AbstractValidator<GetDialogQuery>
{
    public GetDialogQueryValidator()
    {
        RuleFor(x => x.EndUserId)
            .Must(x => PartyIdentifier.TryParse(x, out var id) &&
                       id is NorwegianPersonIdentifier)
            .WithMessage($"{{PropertyName}} must be a valid end user identifier. It must match the format " +
                         $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}'.")
            .When(x => x.EndUserId is not null);
    }
}
