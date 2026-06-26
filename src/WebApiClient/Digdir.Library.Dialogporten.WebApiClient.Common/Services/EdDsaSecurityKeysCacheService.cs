using System.Buffers.Text;
using System.Collections.ObjectModel;
using Altinn.ApiClients.Dialogporten.Common.Exceptions;
using Altinn.ApiClients.Dialogporten.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace Altinn.ApiClients.Dialogporten.Services;

internal interface IEdDsaSecurityKeysCache
{
    ReadOnlyCollection<PublicKeyPair> PublicKeys { get; }
}

internal sealed class DefaultEdDsaSecurityKeysCache : IEdDsaSecurityKeysCache
{
    private List<PublicKeyPair> _publicKeys = [];
    public ReadOnlyCollection<PublicKeyPair> PublicKeys => _publicKeys.AsReadOnly();
    internal void SetPublicKeys(List<PublicKeyPair> publicKeys) => _publicKeys = publicKeys;

    public DefaultEdDsaSecurityKeysCache() { }
    internal DefaultEdDsaSecurityKeysCache(IEnumerable<PublicKeyPair> publicKeys)
    {
        _publicKeys = publicKeys.ToList();
    }
}

internal sealed class EdDsaSecurityKeysCacheService : BackgroundService
{
    private static readonly TimeSpan InitInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ErrorInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(12);

    private readonly PeriodicTimer _timer = new(InitInterval);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EdDsaSecurityKeysCacheService> _logger;
    private readonly DialogportenSettings _settings;
    private readonly DefaultEdDsaSecurityKeysCache _cache;
    private const string EdDsaAlg = "EdDSA";

    public EdDsaSecurityKeysCacheService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EdDsaSecurityKeysCacheService> logger,
        IOptions<DialogportenSettings> settings,
        DefaultEdDsaSecurityKeysCache cache)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _cache = cache;
        _settings = settings.Value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.ThrowOnPublicKeyFetchInit)
        {
            await base.StartAsync(cancellationToken);
            return;
        }

        if (!await TryRefreshAsync(cancellationToken))
        {
            throw new IncompleteDialogportenClientInitializationException("Failed to fetch public keys.");
        }

        _timer.Period = RefreshInterval;
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            _timer.Period = await TryRefreshAsync(cancellationToken)
                ? RefreshInterval
                : ErrorInterval;
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RefreshAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while refreshing the EdDsa keys.");
            return false;
        }

        return true;
    }


    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dialogportenApi = scope.ServiceProvider.GetRequiredService<IInternalDialogportenApi>();
        var jwks = await dialogportenApi.GetJwks(cancellationToken);

        var keys = jwks.Keys
            .Where(x => StringComparer.OrdinalIgnoreCase.Equals(x.Alg, EdDsaAlg))
            .Select(k => new PublicKeyPair(
                k.Kid,
                PublicKey.Import(
                    SignatureAlgorithm.Ed25519,
                    Base64Url.DecodeFromChars(k.X),
                    KeyBlobFormat.RawPublicKey)))
            .ToList();

        _cache.SetPublicKeys(keys);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}

internal sealed record PublicKeyPair(string Kid, PublicKey Key);
