using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

internal sealed class SearchDialogEndUserContextQueryValidator : AbstractValidator<SearchDialogEndUserContextQuery>
{
    public SearchDialogEndUserContextQueryValidator(IOptionsSnapshot<ApplicationSettings> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        var limits = applicationSettings.Value.Limits.ServiceOwnerSearch;

        Include(new PaginationParameterValidator<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>());

        RuleFor(x => x.Party)
            .NotEmpty();

        RuleForEach(x => x.Party)
            .IsValidPartyIdentifier();

        RuleFor(x => x.Party.Count)
            .LessThanOrEqualTo(limits.MaxPartyFilterValues)
            .When(x => x.Party is not null);

        RuleForEach(x => x.Label)
            .IsInEnum();

    }
}
