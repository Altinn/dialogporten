using Altinn.ApiClients.Dialogporten.Features.V1;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common;

internal sealed class EphemeralDialogDecorator(IServiceownerApi serviceownerApi)
    : ServicerOwnerApiDecoratorBase(serviceownerApi)
{
    public override Task<IApiResponse<string>> V1ServiceOwnerDialogsCommandsCreateDialog(V1ServiceOwnerDialogsCommandsCreate_Dialog dto,
        CancellationToken cancellationToken = default)
    {
        const string sentinelLabel = E2EConstants.EphemeralDialogUrn;

        dto.ServiceOwnerContext ??= new();
        var labels = dto.ServiceOwnerContext.ServiceOwnerLabels ??= [];

        if (labels.All(label => label.Value != sentinelLabel))
        {
            labels.Add(new()
            {
                Value = sentinelLabel
            });
        }

        return base.V1ServiceOwnerDialogsCommandsCreateDialog(dto, cancellationToken);
    }
}
