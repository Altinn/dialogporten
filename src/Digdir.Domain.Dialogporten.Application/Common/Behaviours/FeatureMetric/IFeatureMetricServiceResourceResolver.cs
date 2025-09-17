using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IFeatureMetricServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Simple cache interface for dialog service resource caching
/// </summary>
public interface IFeatureMetricServiceResourceCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for requests that operate on a specific dialog by ID
/// </summary>
public interface IFeatureMetricServiceResourceThroughDialogIdRequest
{
    /// <summary>
    /// The ID of the dialog being requested
    /// </summary>
    Guid DialogId { get; }
}

/// <summary>
/// Generic resolver for any IFeatureMetricsServiceResourceThroughDialogIdRequest that can resolve service resource information from dialog ID
/// </summary>
internal sealed class FeatureMetricServiceResourceThroughDialogIdRequestResolver(IDialogDbContext db, IResourceRegistry resourceRegistry, IFeatureMetricServiceResourceCache cache) : IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceThroughDialogIdRequest>
{
    private static readonly string CacheKeyPrefix = "feature-metrics-service-resource:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ServiceResourceInformation?> Resolve(IFeatureMetricServiceResourceThroughDialogIdRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.DialogId}";

        // Try cache first
        var cachedServiceResource = await cache.GetAsync(cacheKey, cancellationToken);
        if (cachedServiceResource != null)
        {
            return await resourceRegistry.GetResourceInformation(cachedServiceResource, cancellationToken);
        }

        // Cache miss - hit database
        var serviceResource = await db.Dialogs
            .Where(x => request.DialogId == x.Id)
            .Select(x => x.ServiceResource)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceResource != null)
        {
            await cache.SetAsync(cacheKey, serviceResource, CacheTtl, cancellationToken);
            return await resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);
        }

        return null;
    }
}

internal interface IFeatureMetricServiceResourceRequest
{
    string ServiceResource { get; }
}

internal sealed class FeatureMetricServiceResourceRequestResolver(IResourceRegistry resourceRegistry) :
    IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceRequest>
{
    public Task<ServiceResourceInformation?> Resolve(IFeatureMetricServiceResourceRequest request, CancellationToken cancellationToken) =>
        resourceRegistry.GetResourceInformation(request.ServiceResource, cancellationToken);
}

internal interface IFeatureMetricServiceResourceIgnoreRequest;

internal sealed class FeatureMetricServiceResourceIgnoreRequestResolver :
    IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceIgnoreRequest>
{
    public Task<ServiceResourceInformation?> Resolve(
        IFeatureMetricServiceResourceIgnoreRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(null);
}
