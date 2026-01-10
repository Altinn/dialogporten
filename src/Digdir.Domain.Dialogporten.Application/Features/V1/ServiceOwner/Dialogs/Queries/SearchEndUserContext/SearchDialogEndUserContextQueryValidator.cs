using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

internal sealed class SearchDialogEndUserContextQueryValidator : AbstractValidator<SearchDialogEndUserContextQuery>
{
    public SearchDialogEndUserContextQueryValidator()
    {
        Include(new PaginationParameterValidator<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>());

        RuleFor(x => x.Party)
            .NotEmpty();

        RuleForEach(x => x.Party)
            .IsValidPartyIdentifier();

        RuleFor(x => x.Party.Count)
            .LessThanOrEqualTo(20)
            .When(x => x.Party is not null);

        RuleFor(x => x.EndUserId)
            .Must(x => PartyIdentifier.TryParse(x, out var id) && id is NorwegianPersonIdentifier or SystemUserIdentifier)
            .WithMessage($"{{PropertyName}} must be a valid end user identifier. It must match the format " +
                         $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}' or '{SystemUserIdentifier.PrefixWithSeparator}{{uuid}}'.")
            .When(x => x.EndUserId is not null);

        RuleForEach(x => x.Label)
            .IsInEnum();

        RuleFor(x => x.OrderBy)
            .Must(orderBy => orderBy is null || orderBy.GetOrderString() ==
                OrderSet<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>.Default.GetOrderString())
            .WithMessage("'OrderBy' must use the default ordering (contentUpdatedAt desc, id desc).");
    }
}
