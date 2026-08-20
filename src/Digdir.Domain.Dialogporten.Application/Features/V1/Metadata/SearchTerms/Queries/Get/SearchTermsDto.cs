using Digdir.Domain.Dialogporten.Domain.SearchTerms;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;

public sealed class SearchTermsDto
{
    /// <summary>The resolved language of this document (e.g. <c>nb</c>, <c>nn</c>, <c>en</c>).</summary>
    public required string Language { get; init; }

    /// <summary>When the underlying generation run produced this document.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// The curated search terms, each with the unprefixed resource identifiers
    /// (without <c>urn:altinn:resource:</c>) the term appears in, sorted.
    /// </summary>
    public required IReadOnlyList<SearchTermEntry> Words { get; init; }
}
