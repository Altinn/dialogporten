namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class Words
{
    internal static readonly string[]
        English = File.Exists("./wordlist_en") ? File.ReadAllLines("./wordlist_en") : [];

    internal static readonly string[]
        Norwegian = File.Exists("./wordlist_no") ? File.ReadAllLines("./wordlist_no") : [];

    static Words()
    {
        if (English.Length > Norwegian.Length)
        {
            English = English.Except(Norwegian).ToArray();
        }
        else
        {
            Norwegian = Norwegian.Except(English).ToArray();
        }
    }
}
