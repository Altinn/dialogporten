namespace Digdir.Domain.Dialogporten.Application.Features.V1.Wordlist.Filtering;

public interface IWordlistFilter
{
    bool ShouldKeep(string word, int minLength);
}
