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

    /// <summary>
    /// Maskinporten settings used to authenticate requests to Dialogporten.
    /// </summary>
    /// <remarks>
    /// Only required when using the built-in Maskinporten authentication. When registering the client with a
    /// custom authentication setup (see the <c>AddDialogportenClient</c> overload that accepts an
    /// <see cref="Microsoft.Extensions.DependencyInjection.IHttpClientBuilder"/> configuration delegate),
    /// this can be left unset and the caller is responsible for attaching authentication.
    /// </remarks>
    public MaskinportenSettings? Maskinporten { get; set; }

    internal static bool Validate() => true;
}
