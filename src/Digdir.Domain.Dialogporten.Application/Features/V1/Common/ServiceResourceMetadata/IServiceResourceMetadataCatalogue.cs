namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

/// <summary>
/// A single entry in the shared service-resource metadata catalogue: the resource's full URN paired with its
/// fully-built, all-language metadata item. Items are immutable and shared across callers — apply per-request
/// language pruning with <see cref="ServiceResourceMetadataPruneExtensions.PrunedCopy"/> (which copies), never
/// by mutating the item in place.
/// </summary>
public sealed record ServiceResourceMetadataCatalogueEntry(string ResourceUrn, ServiceResourceMetadataItemDto Item);

/// <summary>
/// Provides the caller-independent catalogue of all Dialogporten-referenced service resources, built once and
/// cached (the per-resource metadata DTO depends only on the resource id; only language pruning varies per
/// request). Both the public-catalogue query and the authorized-resources query select from this catalogue,
/// so the expensive metadata construction runs once per cache window instead of per request.
/// </summary>
public interface IServiceResourceMetadataCatalogue
{
    Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(CancellationToken cancellationToken);
}
