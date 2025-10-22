using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// Resolves service resource information for feature metric tracking based on request type
/// </summary>
/// <typeparam name="TRequest">The type of request to resolve service resource information for</typeparam>
internal interface IFeatureMetricServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Cache interface for dialog service resource information used in feature metric tracking
/// </summary>
public interface IFeatureMetricServiceResourceCache
{
    Task<ServiceResourceInformation?> GetServiceResource(Guid dialogId, CancellationToken cancellationToken);
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
/// Resolver for requests that have a DialogId property.
/// Looks up service resource information by first finding the dialog, then extracting its service resource.
/// </summary>
internal sealed class FeatureMetricServiceResourceThroughDialogIdRequestResolver(IFeatureMetricServiceResourceCache cache) : IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceThroughDialogIdRequest>
{
    public async Task<ServiceResourceInformation?> Resolve(IFeatureMetricServiceResourceThroughDialogIdRequest request, CancellationToken cancellationToken)
    {
        return await cache.GetServiceResource(request.DialogId, cancellationToken);
    }
}

/// <summary>
/// Marker interface for requests that have a ServiceResource property but no DialogId.
/// Used by the feature metrics system to directly resolve service resource information.
/// </summary>
internal interface IFeatureMetricServiceResourceRequest
{
    /// <summary>
    /// The service resource identifier
    /// </summary>
    string ServiceResource { get; }
}

/// <summary>
/// Resolver for requests that have a ServiceResource property.
/// Directly looks up service resource information from the resource registry.
/// </summary>
internal sealed class FeatureMetricServiceResourceRequestResolver(IResourceRegistry resourceRegistry) :
    IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceRequest>
{
    public Task<ServiceResourceInformation?> Resolve(IFeatureMetricServiceResourceRequest request, CancellationToken cancellationToken) =>
        resourceRegistry.GetResourceInformation(request.ServiceResource, cancellationToken);
}

/// <summary>
/// Marker interface for requests that should not be tracked by the feature metrics system.
/// Used for requests where service resource tracking is not applicable or desired.
/// </summary>
internal interface IFeatureMetricServiceResourceIgnoreRequest;

/// <summary>
/// Resolver for requests that should not be tracked by the feature metrics system.
/// Always returns null to indicate no service resource information should be collected.
/// </summary>
internal sealed class FeatureMetricServiceResourceIgnoreRequestResolver :
    IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceIgnoreRequest>
{
    public Task<ServiceResourceInformation?> Resolve(
        IFeatureMetricServiceResourceIgnoreRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(null);
}
