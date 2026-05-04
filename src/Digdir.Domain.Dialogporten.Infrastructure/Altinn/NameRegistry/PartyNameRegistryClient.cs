using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal sealed class PartyNameRegistryClient : IPartyNameRegistry
{
    private readonly IFusionCache _cache;
    private readonly HttpClient _client;
    private readonly ILogger<PartyNameRegistryClient> _logger;
    private readonly IOptionsMonitor<ApplicationSettings> _applicationSettings;
    private bool _useCorrectPersonNameOrdering;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public PartyNameRegistryClient(
        HttpClient client,
        IFusionCacheProvider cacheProvider,
        ILogger<PartyNameRegistryClient> logger,
        IOptionsMonitor<ApplicationSettings> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(applicationSettings);

        var cache = cacheProvider.GetCache(nameof(NameRegistry));
        ArgumentNullException.ThrowIfNull(cache);

        _client = client;
        _logger = logger;
        _cache = cache;
        _applicationSettings = applicationSettings;
    }

    public async Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<string?>(
            GetCacheKey(externalIdWithPrefix),
            GetNameFactory(externalIdWithPrefix),
            token: cancellationToken);

    private Func<FusionCacheFactoryExecutionContext<string?>, CancellationToken, Task<string?>> GetNameFactory(
        string externalIdWithPrefix
    )
    {
        return async (ctx, ct) =>
        {
            var name = await GetNameFromRegister(externalIdWithPrefix, ct);
            if (name is null)
            {
                // Short negative cache
                ctx.Options.Duration = TimeSpan.FromSeconds(10);
            }

            return name;
        };
    }

    public void CacheNames(Dictionary<string, string> actorIdToName)
    {
        foreach (var (actorId, name) in actorIdToName)
        {
            if (PartyIdentifier.TryParse(actorId, out var partyIdentifier))
            {
                _cache.Set(GetCacheKey(actorId), FlipNameIfPerson(partyIdentifier, name));
            }
        }
    }

    private string GetCacheKey(string externalIdWithPrefix)
    {
        // Use a instance member to ensure we use the same value in the factory method
        _useCorrectPersonNameOrdering = _applicationSettings.CurrentValue.FeatureToggle.UseCorrectPersonNameOrdering;
        return $"Name{(_useCorrectPersonNameOrdering ? "_v2" : "")}_{externalIdWithPrefix}";
    }

    private async Task<string?> GetNameFromRegister(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
        {
            return null;
        }

        // We do not have any information about system users, self-identified users or Feide users in the party name registry
        switch (partyIdentifier)
        {
            case AltinnSelfIdentifiedUserIdentifier or IdportenEmailUserIdentifier:
                return partyIdentifier.Id;
            case FeideUserIdentifier:
                return $"Feide User ({partyIdentifier.Id[..6]})";
            case SystemUserIdentifier:
                // TODO! When called within enduser context (ie. we have a ClaimsPrincipal), we use the systemuser_org to look up name
                // In other contexts, eg. PopulateActorNameInterceptor, this information is not available. At some point, we should have
                // some sort of lookup mechanism here
                return "System User";
            default:
                // Handle below
                break;
        }

        if (!TryGetLookupDto(partyIdentifier, out var nameLookup))
        {
            return null;
        }

        const string apiUrl = "register/api/v1/dialogporten/parties/query";
        var nameLookupResult = await _client.PostAsJsonEnsuredAsync<NameLookupResult>(
            apiUrl,
            nameLookup,
            serializerOptions: SerializerOptions,
            cancellationToken: cancellationToken);

        var name = nameLookupResult.Data.FirstOrDefault()?.DisplayName;
        if (name is null)
        {
            // This is PII, but this is an error condition (probably due to missing Altinn profile)
            _logger.LogError("Failed to get name from party name registry for external id {ExternalId}", externalIdWithPrefix);
            return null;
        }

        // TODO! Currently, arbeidsflate expects the name ordering to be "Last First" for Norwegian persons, and does
        // the flip itself for persons. See https://github.com/Altinn/dialogporten/issues/3171
        name = FlipNameIfPerson(partyIdentifier, name);

        return name;
    }

    private string FlipNameIfPerson(IPartyIdentifier partyIdentifier, string name)
    {
        if (!_useCorrectPersonNameOrdering && partyIdentifier is NorwegianPersonIdentifier)
        {
            // Flip the order of the name parts: "A B C" -> "C A B" / "A B" -> "B A"
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                name = $"{parts[^1]} {string.Join(" ", parts[..^1])}";
            }
        }

        return name;
    }

    private static bool TryGetLookupDto(IPartyIdentifier partyIdentifier, [NotNullWhen(true)] out NameLookup? nameLookup)
    {

        nameLookup = partyIdentifier switch
        {
            NorwegianPersonIdentifier personIdentifier => new() { Data = [personIdentifier.FullId] },
            NorwegianOrganizationIdentifier organizationIdentifier => new() { Data = [organizationIdentifier.FullId] },
            _ => null
        };

        return nameLookup is not null;
    }

    private sealed class NameLookup
    {
        public List<string> Data { get; set; } = null!;
    }

    private sealed class NameLookupResult
    {
        public List<NameLookupEntry> Data { get; set; } = null!;
    }

    private sealed class NameLookupEntry
    {
        public string? DisplayName { get; set; }
    }
}

internal sealed class LocalPartNameRegistryClient : IPartyNameRegistry
{
    private readonly Dictionary<string, string> _fakeCache = new();
    public Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        return _fakeCache.TryGetValue(externalIdWithPrefix, out var cachedName)
            ? Task.FromResult<string?>(cachedName)
            : Task.FromResult<string?>("Gunnar Gunnarson");
    }

    public void CacheNames(Dictionary<string, string> idToName)
    {
        foreach (var keyValuePair in idToName)
        {
            _fakeCache.Remove(keyValuePair.Key);
            _fakeCache.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }
}
