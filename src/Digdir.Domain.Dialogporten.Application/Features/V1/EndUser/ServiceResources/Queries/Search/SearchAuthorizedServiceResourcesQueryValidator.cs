using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;

internal sealed class SearchAuthorizedServiceResourcesQueryValidator : AbstractValidator<SearchAuthorizedServiceResourcesQuery>
{
    public SearchAuthorizedServiceResourcesQueryValidator(IOptionsSnapshot<ApplicationSettings> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        var limits = applicationSettings.Value.Limits.EndUserSearch;

        // Bound the work of a filtered request: the resolved set is computed per filtered party, so cap the
        // number of party filter values (reusing the end-user search limit). Unfiltered requests are bounded
        // separately by the full-catalogue fallback in the provider.
        RuleFor(x => x.Parties!.Length)
            .LessThanOrEqualTo(limits.MaxPartyFilterValues)
            .When(x => x.Parties is not null);

        // Reject blank/invalid party values. Without this, a supplied-but-blank filter (e.g. ?party= or
        // parties: [""]) is normalized away to null in the provider and silently treated as an UNFILTERED
        // request, which would return all authorized resources (or the full catalogue for whale callers).
        RuleForEach(x => x.Parties)
            .IsValidPartyIdentifier()
            .When(x => x.Parties is not null);
    }
}
