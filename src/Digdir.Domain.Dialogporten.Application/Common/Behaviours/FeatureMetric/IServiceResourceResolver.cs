using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
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
internal sealed class DialogQueryResolver(IDialogDbContext db, IResourceRegistry resourceRegistry) : IServiceResourceResolver<IDialogIdQuery>
{
    public async Task<ServiceResourceInformation?> Resolve(IDialogIdQuery request, CancellationToken cancellationToken)
    {
        // TODO: Cache results?
        var serviceResource = await db.Dialogs
            .Where(x => request.DialogId == x.Id)
            .Select(x => x.ServiceResource)
            .FirstOrDefaultAsync(cancellationToken);

        return serviceResource is null ? null
            : await resourceRegistry.GetResourceInformation(serviceResource, cancellationToken);
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
