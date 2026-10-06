using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.Domain.SearchTerms;

/// <summary>
/// The terse serialization contract for a single entry in <see cref="SearchTermList.Words"/>:
/// <c>{ "w": canonical word, "s": [sorted unprefixed resource ids] }</c>. This one type is used
/// both when the Janitor generates the jsonb documents and when the metadata endpoint reads them,
/// so the write and read sides cannot drift apart.
/// </summary>
public sealed record SearchTermEntry(
    [property: JsonPropertyName("w")] string Word,
    [property: JsonPropertyName("s")] IReadOnlyList<string> Resources);
