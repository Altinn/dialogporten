using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Root interface for the ServiceOwner API client, providing access to versioned APIs.
/// </summary>
public interface IServiceOwnerApi
{
    /// <summary>
    /// Gets the V1 ServiceOwner API.
    /// </summary>
    IServiceOwnerV1 V1 { get; }
}
