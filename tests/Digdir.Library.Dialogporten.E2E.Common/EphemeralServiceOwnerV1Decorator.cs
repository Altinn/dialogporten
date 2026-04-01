using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common;

public sealed class EphemeralServiceOwnerV1Decorator(IServiceOwnerV1 serviceOwnerV1)
    : ServiceOwnerV1DecoratorBase(serviceOwnerV1)
{
    public override Task<IApiResponse<string>> CreateDialog(CreateDialogRequest request, CancellationToken cancellationToken = default)
    {
        const string sentinelLabel = E2EConstants.EphemeralDialogUrn;

        request.ServiceOwnerContext ??= new();
        var labels = request.ServiceOwnerContext.ServiceOwnerLabels ??= [];

        if (labels.All(label => label.Value != sentinelLabel))
        {
            labels.Add(new()
            {
                Value = sentinelLabel
            });
        }

        return base.CreateDialog(request, cancellationToken);
    }
}
