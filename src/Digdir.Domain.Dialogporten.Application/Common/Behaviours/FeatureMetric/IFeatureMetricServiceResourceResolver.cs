using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
/// Generic resolver for any IFeatureMetricsServiceResourceThroughDialogIdRequest that can resolve service resource information from dialog ID
/// </summary>
internal sealed class FeatureMetricServiceResourceThroughDialogIdRequestResolver(IFeatureMetricServiceResourceCache cache) : IFeatureMetricServiceResourceResolver<IFeatureMetricServiceResourceThroughDialogIdRequest>
{
    public async Task<ServiceResourceInformation?> Resolve(IFeatureMetricServiceResourceThroughDialogIdRequest request, CancellationToken cancellationToken)
    {
        return await cache.GetServiceResource(request.DialogId, cancellationToken);
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
