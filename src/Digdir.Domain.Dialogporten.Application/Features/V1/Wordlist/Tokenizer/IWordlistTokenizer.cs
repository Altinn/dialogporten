namespace Digdir.Domain.Dialogporten.Application.Features.V1.Wordlist.Tokenizer;

public interface IWordlistTokenizer
{
    HashSet<string> Tokenize(string text);
}
