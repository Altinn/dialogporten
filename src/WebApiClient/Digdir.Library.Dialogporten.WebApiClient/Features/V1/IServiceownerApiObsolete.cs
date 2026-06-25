using Refit;

namespace Altinn.ApiClients.Dialogporten.Features.V1;

public partial interface IServiceownerApi
{
    [Obsolete("Renamed to fix a typo. Use V1ServiceOwnerDialogsQueriesGetTransmissionDialogTransmission instead. This method will be removed in a future major version.")]
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetTransmission_Transmission>>
        V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(
            Guid dialogId,
            Guid transmissionId,
            CancellationToken cancellationToken = default)
        => V1ServiceOwnerDialogsQueriesGetTransmissionDialogTransmission(
            dialogId,
            transmissionId,
            cancellationToken);
}
