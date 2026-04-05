using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <inheritdoc />
public sealed class ServiceOwnerClient : IServiceOwnerClient
{
    /// <inheritdoc />
    public IServiceOwnerV1 V1 { get; }

    public ServiceOwnerClient(IServiceOwnerV1 v1)
    {
        V1 = v1;
    }
}
