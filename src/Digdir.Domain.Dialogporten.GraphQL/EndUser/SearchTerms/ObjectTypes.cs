namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms;

public sealed class SearchTerms
{
    public string Language { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; }
    public List<SearchTerm> Words { get; set; } = [];
}

public sealed class SearchTerm
{
    public string Word { get; set; } = null!;
    public List<string> Resources { get; set; } = [];
}
