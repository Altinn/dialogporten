using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts;
using Refit;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1;

/// <summary>Gets service resources currently in use in Dialogporten.</summary>
public interface IMetadataApi
{
    /// <summary>Gets service resources currently in use in Dialogporten.</summary>
    /// <remarks>Returns public service resource metadata with related service owner, role, and access package metadata.</remarks>
    /// <param name="acceptLanguage">accept_Language parameter</param>
    /// <param name="cancellationToken">The cancellation token to cancel the request.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>200</term>
    /// <description>Service resource metadata.</description>
    /// </item>
    /// <item>
    /// <term>503</term>
    /// <description>Service Unavailable, used when Dialogporten is in maintenance mode</description>
    /// </item>
    /// </list>
    /// </returns>
    [Headers("Accept: application/json, text/plain")]
    [Get("/api/v1/metadata/serviceresources")]
    Task<IApiResponse<ServiceResourceMetadataList>> GetServiceResourceMetadata([Header("accept-Language")] AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default);

    /// <summary>Gets currently enforced application-level query limits</summary>
    /// <remarks>Returns the active limits for EndUser and ServiceOwner search filters.</remarks>
    /// <param name="cancellationToken">The cancellation token to cancel the request.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>200</term>
    /// <description>The currently enforced application-level query limits.</description>
    /// </item>
    /// <item>
    /// <term>503</term>
    /// <description>Service Unavailable, used when Dialogporten is in maintenance mode</description>
    /// </item>
    /// </list>
    /// </returns>
    [Headers("Accept: application/json, text/plain")]
    [Get("/api/v1/metadata/limits")]
    Task<IApiResponse<Limits>> GetLimits(CancellationToken cancellationToken = default);
}
