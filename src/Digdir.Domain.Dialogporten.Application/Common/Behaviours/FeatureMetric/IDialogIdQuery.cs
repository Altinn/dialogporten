using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

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
internal sealed class DialogQueryResolver(IDialogDbContext db) : IServiceResourceResolver<IDialogIdQuery>
{
    public async Task<ServiceResourceInformation?> Resolve(IDialogIdQuery request, CancellationToken cancellationToken)
    {
        // TODO: Cache results?
        var dialog = await db.Dialogs
            .Where(x => request.DialogId == x.Id)
            .Select(x => new { x.ServiceResource, x.ServiceResourceType, x.Org })
            .FirstOrDefaultAsync(cancellationToken);

        return dialog == null
            ? null
            : new ServiceResourceInformation(
                dialog.ServiceResource,
                dialog.ServiceResourceType,
                string.Empty, // OwnerOrgNumber - would need to be looked up
                dialog.Org);
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
