using Altinn.ApiClients.Maskinporten.Config;

namespace Altinn.ApiClients.Dialogporten;

public sealed class DialogportenSettings
{
    /// <summary>
    /// The base URI for the dialogporten endpoint, up to but excluding "/api/v...".
    /// For example the base URI of 'https://altinn-tt02-api.azure-api.net/dialogporten/api/v1/serviceowner/dialogs'
    /// is 'https://altinn-tt02-api.azure-api.net/dialogporten'.
    /// </summary>
    public string BaseUri { get; set; } = null!;

    /// <summary>
    /// If true, the library will throw an exception if it cannot fetch public keys from dialogporten .wellKnown endpoint.
    /// </summary>
    /// <remarks>
    /// Default is true.
    /// </remarks>
    public bool ThrowOnPublicKeyFetchInit { get; set; } = true;

    public MaskinportenSettings Maskinporten { get; set; } = null!;

    internal static bool Validate() => true;
}
