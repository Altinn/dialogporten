namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <inheritdoc />
public sealed class ServiceOwnerApi : IServiceOwnerApi
{
    /// <inheritdoc />
    public Features.V1.IServiceownerApi V1 { get; }

    public ServiceOwnerApi(Features.V1.IServiceownerApi v1)
    {
        V1 = v1;
    }
}
