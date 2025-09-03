using Refit;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Infrastructure;

public interface IGithubClient
{
    [Get("/Ondkloss/norwegian-wordlist/raw/refs/heads/master/wordlist_20220201_norsk_ordbank_nno_2012.txt")]
    Task<string> GetNorwegianWordList();

    [Get("/dwyl/english-words/raw/refs/heads/master/words.txt")]
    Task<string> GetEnglishWordList();
}
