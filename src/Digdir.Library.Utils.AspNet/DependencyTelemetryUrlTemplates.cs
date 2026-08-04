using System.Text.RegularExpressions;

namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// Url-template defaults for outgoing HTTP dependencies. Collapses high-cardinality
/// per-id URLs into a single templated <c>AppDependencies.Name</c> row so telemetry
/// aggregations (e.g. <c>summarize count() by name</c>) stay useful.
///
/// Add a new entry below per outbound HttpClient whose URL embeds an id.
/// Each entry pairs a path-matching regex with the templated path to emit.
/// </summary>
public static class DependencyTelemetryUrlTemplates
{
    private const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    // -- Altinn Resource Registry --
    // ResourceRegistryClient: GET .../resourceregistry/api/v1/resource/{id}/policy
    private static readonly Regex ResourceRegistryPolicyRegex = new(
        @"/resourceregistry/api/v1/resource/[^/?#]+/policy/?$",
        DefaultOptions);

    // ResourceRegistryClient / MetadataLinkProvider: GET .../resourceregistry/api/v1/resource/{id}.
    // Excludes the sibling literal paths /resource/resourcelist and /resource/updated so those
    // keep their distinct, low-cardinality dependency names.
    private static readonly Regex ResourceRegistryMetadataRegex = new(
        @"/resourceregistry/api/v1/resource/(?!(?:resourcelist|updated)(?:/|$))[^/?#]+/?$",
        DefaultOptions);

    public static IReadOnlyList<HttpDependencyUrlTemplate> Defaults { get; } =
    [
        new(ResourceRegistryPolicyRegex, "resourceregistry/api/v1/resource/{resourceId}/policy"),
        new(ResourceRegistryMetadataRegex, "resourceregistry/api/v1/resource/{resourceId}")
    ];
}
