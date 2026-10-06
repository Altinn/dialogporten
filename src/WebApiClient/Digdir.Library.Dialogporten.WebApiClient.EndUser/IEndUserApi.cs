using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;

namespace Altinn.ApiClients.Dialogporten.EndUser;

/// <summary>
/// Root interface for the EndUser API client, providing access to versioned APIs.
/// </summary>
public interface IEndUserApi
{
    /// <summary>
    /// Gets the V1 EndUser API.
    /// </summary>
    IEnduserApi V1 { get; }
}
