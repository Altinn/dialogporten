using Digdir.Library.Entity.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.SearchTerms;

/// <summary>
/// A persisted, per-language curated search-term document, generated offline by the Janitor
/// <c>generate-searchterms</c> command and served from <c>/api/v1/metadata/searchterms</c>.
/// One row per language; all rows of a single generation run share the same <see cref="GeneratedAt"/>.
/// </summary>
public sealed class SearchTermList : IEntity
{
    public Guid Id { get; set; }

    /// <summary>Normalized two-letter language code (e.g. <c>nb</c>, <c>nn</c>, <c>en</c>). Unique.</summary>
    public required string Language { get; set; }

    /// <summary>When this generation run produced the document. Drives the served ETag / Last-Modified.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// The terse words array stored verbatim as jsonb: <c>[{ "w": "skattemelding", "s": ["app_skd_x"] }]</c>
    /// where <c>w</c> is the canonical word and <c>s</c> the sorted unprefixed resource identifiers.
    /// </summary>
    public required string Words { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
