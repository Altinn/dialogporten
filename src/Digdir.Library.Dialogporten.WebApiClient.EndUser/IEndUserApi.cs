namespace Altinn.ApiClients.Dialogporten.EndUser;

/// <summary>
/// Root interface for the EndUser API client, providing access to versioned APIs.
/// </summary>
public interface IEndUserApi
{
    /// <summary>
    /// Gets the V1 EndUser API.
    /// </summary>
    Features.V1.IEnduserApi V1 { get; }
}
