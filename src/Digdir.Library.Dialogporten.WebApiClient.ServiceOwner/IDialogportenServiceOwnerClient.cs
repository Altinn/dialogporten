namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Entry point for Dialogporten service owner API operations.
/// </summary>
public interface IDialogportenServiceOwnerClient
{
    /// <summary>
    /// Service owner operations for the V1 API.
    /// </summary>
    IDialogportenServiceOwnerV1 V1 { get; }
}
