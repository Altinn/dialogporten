using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Simple cache interface for dialog service resource caching
/// </summary>
public interface IDialogServiceResourceCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for queries that operate on a specific dialog by ID
/// </summary>
public interface IDialogIdQuery
{
    /// <summary>
    /// The ID of the dialog being queried
    /// </summary>
    Guid DialogId { get; }
}

/// <summary>
/// Generic resolver for any IDialogIdQuery that can resolve service resource information from dialog ID
/// </summary>
internal sealed class DialogQueryResolver(IDialogDbContext db, IResourceRegistry resourceRegistry, IDialogServiceResourceCache cache) : IServiceResourceResolver<IDialogIdQuery>
{
    private static readonly string CacheKeyPrefix = "dialog-service-resource:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ServiceResourceInformation?> Resolve(IDialogIdQuery request, CancellationToken cancellationToken)
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

internal interface IServiceResourceQuery
{
    string ServiceResource { get; }
}

internal sealed class ServiceResourceQueryResolver(IResourceRegistry resourceRegistry) :
    IServiceResourceResolver<IServiceResourceQuery>
{
    public Task<ServiceResourceInformation?> Resolve(IServiceResourceQuery request, CancellationToken cancellationToken) =>
        resourceRegistry.GetResourceInformation(request.ServiceResource, cancellationToken);
}

internal interface IDoNotCareAboutServiceResource;

internal sealed class DoNotCareAboutServiceResourceResolver :
    IServiceResourceResolver<IDoNotCareAboutServiceResource>
{
    public Task<ServiceResourceInformation?> Resolve(
        IDoNotCareAboutServiceResource request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(null);
}
