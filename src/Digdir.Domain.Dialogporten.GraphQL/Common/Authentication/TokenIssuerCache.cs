using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;

public interface ITokenIssuerCache
{
    Task<string?> GetIssuerForScheme(string schemeName);
}

public sealed class TokenIssuerCache : ITokenIssuerCache, IDisposable
{
    private readonly Dictionary<string, string> _issuerMappings = [];
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private bool _initialized;
    private readonly IReadOnlyCollection<JwtBearerTokenSchemasOptions> _jwtTokenSchemas;

    public TokenIssuerCache(IOptions<GraphQlSettings> apiSettings)
    {
        _jwtTokenSchemas = apiSettings
            .Value
            .Authentication
            .JwtBearerTokenSchemas
            ?? throw new ArgumentException("JwtBearerTokenSchemas is required.");
    }

    public async Task<string?> GetIssuerForScheme(string schemeName)
    {
        await EnsureInitializedAsync();

        return _issuerMappings.TryGetValue(schemeName, out var issuer)
            ? issuer : null;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationSemaphore.WaitAsync();

        try
        {
            if (_initialized)
            {
                return;
            }

            foreach (var schema in _jwtTokenSchemas)
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    schema.WellKnown, new OpenIdConnectConfigurationRetriever());
                var config = await configManager.GetConfigurationAsync();
                _issuerMappings[schema.Name] = config.Issuer;
            }

            _initialized = true;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    public void Dispose() => _initializationSemaphore.Dispose();
}
