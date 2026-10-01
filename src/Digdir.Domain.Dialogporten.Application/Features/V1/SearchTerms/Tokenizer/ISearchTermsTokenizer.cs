namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Tokenizer;

public interface ISearchTermsTokenizer
{
    HashSet<string> Tokenize(string text);
}
