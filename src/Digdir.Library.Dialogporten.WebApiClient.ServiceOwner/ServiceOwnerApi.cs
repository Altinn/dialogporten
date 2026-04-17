using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <inheritdoc />
public sealed class ServiceOwnerApi : IServiceOwnerApi
{
    /// <inheritdoc />
    public IServiceOwnerV1 V1 { get; }

    public ServiceOwnerApi(IServiceOwnerV1 v1)
    {
        V1 = v1;
    }
}
