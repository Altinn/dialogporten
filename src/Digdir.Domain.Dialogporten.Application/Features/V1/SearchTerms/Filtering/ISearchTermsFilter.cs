namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;

public interface ISearchTermsFilter
{
    bool ShouldKeep(string word, int minLength);

    /// <summary>
    /// The merged stopword set (all bundled stoplists). Used for the exact surface-form
    /// match in <see cref="ShouldKeep"/>.
    /// </summary>
    IReadOnlyCollection<string> Stopwords { get; }

    /// <summary>
    /// The stopwords of the bundled stoplist covering <paramref name="language"/> only
    /// (e.g. <c>no.txt</c> for nb/nn, <c>en.txt</c> for en). Use this — never the merged
    /// <see cref="Stopwords"/> — when matching stopwords by stem: stemming another language's
    /// stopwords with this language's dictionary yields stems that collide with legitimate words.
    /// Returns an empty set for languages without a bundled list.
    /// </summary>
    IReadOnlyCollection<string> StopwordsForLanguage(string language);
}
