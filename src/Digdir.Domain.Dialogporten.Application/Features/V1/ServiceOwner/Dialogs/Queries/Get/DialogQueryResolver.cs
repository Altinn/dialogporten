using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

/// <summary>
/// Generic resolver for any IDialogIdQuery that can resolve service resource information from dialog ID
/// </summary>
internal sealed class DialogQueryResolver(IDialogDbContext db) : IServiceResourceResolver<IDialogIdQuery>
{
    public async Task<ServiceResourceInformation?> Resolve(IDialogIdQuery request, CancellationToken cancellationToken)
    {
        var dialog = await db.Dialogs
            .Where(x => request.DialogId == x.Id)
            .Select(x => new { x.ServiceResource, x.Org })
            .FirstOrDefaultAsync(cancellationToken);

        if (dialog == null)
            return null;

        return new ServiceResourceInformation(
            dialog.ServiceResource,
            "dialog", // ResourceType - can be made more specific later
            string.Empty, // OwnerOrgNumber - would need to be looked up
            dialog.Org);
    }
}
