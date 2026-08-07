namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;

public interface ISearchTermsFilter
{
    bool ShouldKeep(string word, int minLength);

    /// <summary>
    /// The merged stopword set (all bundled stoplists). Exposed so callers can match
    /// stopwords by stem in addition to the exact surface-form match in <see cref="ShouldKeep"/>.
    /// </summary>
    IReadOnlyCollection<string> Stopwords { get; }
}
