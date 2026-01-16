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
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cacheProvider.GetCache(nameof(NameRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
    }

    public async Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync<string?>(
            GetCacheKey(externalIdWithPrefix),
            async (ctx, ct) =>
            {
                var name = await GetNameFromRegister(externalIdWithPrefix, ct);
                if (name is null)
                {
                    // Short negative cache
                    ctx.Options.Duration = TimeSpan.FromSeconds(10);
                }

                return name;
            },
            token: cancellationToken);

    private string GetCacheKey(string externalIdWithPrefix)
    {
        // Use a instance member to ensure we use the same value in the factory method
        _useCorrectPersonNameOrdering = _applicationSettings.CurrentValue.FeatureToggle.UseCorrectPersonNameOrdering;
        return $"Name{(_useCorrectPersonNameOrdering ? "_v2" : "")}_{externalIdWithPrefix}";
    }

    private async Task<string?> GetNameFromRegister(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        const string apiUrl = "register/api/v1/dialogporten/parties/query";

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
        if (!_useCorrectPersonNameOrdering && partyIdentifier is NorwegianPersonIdentifier)
        {
            // Flip the order of the name parts: "A B C" -> "B C A" / "A B" -> "B A"
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                name = $"{string.Join(" ", parts[1..])} {parts[0]}";
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
    public Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken) => Task.FromResult<string?>("Gunnar Gunnarson");
}
