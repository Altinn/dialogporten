namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchTerms;

public sealed class SearchTerms
{
    public required string Language { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public List<SearchTerm> Words { get; set; } = [];
}

public sealed class SearchTerm
{
    public required string Word { get; set; }
    public List<string> Resources { get; set; } = [];
}
