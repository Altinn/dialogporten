using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Entry point for Dialogporten service owner API operations.
/// </summary>
public interface IServiceOwnerClient
{
    /// <summary>
    /// Service owner operations for the V1 API.
    /// </summary>
    IServiceOwnerV1 V1 { get; }
}
