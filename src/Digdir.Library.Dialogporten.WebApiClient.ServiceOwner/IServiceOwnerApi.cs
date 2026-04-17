namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Root interface for the ServiceOwner API client, providing access to versioned APIs.
/// </summary>
public interface IServiceOwnerApi
{
    /// <summary>
    /// Gets the V1 ServiceOwner API.
    /// </summary>
    Features.V1.IServiceownerApi V1 { get; }
}
