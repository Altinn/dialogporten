using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
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
            .Equal(1)
            .When(x => x.Party is not null);

        RuleForEach(x => x.Label)
            .IsInEnum();

    }
}
