namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;

public interface ISearchTermsFilter
{
    bool ShouldKeep(string word, int minLength);
}
