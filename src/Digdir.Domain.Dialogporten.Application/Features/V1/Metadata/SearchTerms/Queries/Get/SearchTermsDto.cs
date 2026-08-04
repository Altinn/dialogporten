namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;

public sealed class SearchTermsDto
{
    /// <summary>The resolved language of this document (e.g. <c>nb</c>, <c>nn</c>, <c>en</c>).</summary>
    public required string Language { get; init; }

    /// <summary>When the underlying generation run produced this document.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>The curated search terms, each with the resources the term appears in.</summary>
    public required IReadOnlyList<SearchTermDto> Words { get; init; }
}

public sealed class SearchTermDto
{
    /// <summary>The canonical search term.</summary>
    public required string Word { get; init; }

    /// <summary>Unprefixed resource identifiers (without <c>urn:altinn:resource:</c>) the term appears in, sorted.</summary>
    public required IReadOnlyList<string> Resources { get; init; }
}
