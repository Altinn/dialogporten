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
    private readonly IOptionsSnapshot<ApplicationSettings> _appSettings;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public PartyNameRegistryClient(
        HttpClient client,
        IFusionCacheProvider cacheProvider,
        ILogger<PartyNameRegistryClient> logger,
        IOptionsSnapshot<ApplicationSettings> appSettings)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _cache = cacheProvider.GetCache(nameof(NameRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
    }

    public async Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        var name = await _cache.GetOrSetAsync<string?>(
            $"Name_v2_{externalIdWithPrefix}",
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

        // This is to emulate the old behavior, which had incorrect naming convention for parties (First Last instead of Last First)
        // When the feature toggle is enabled, we will use the correct naming convention, which will have to be done _after_
        // the existing data has been corrected manually. After the feature toggle has been enabled and the data corrected, this code can be removed.
        if (name != null
            && name.Contains(' ')
            && PartyIdentifier.TryParse(externalIdWithPrefix, out var personId) && personId is NorwegianPersonIdentifier
            && !_appSettings.Value.FeatureToggle.UseCorrectNamingConventionForParties)
        {
            var nameParts = name.Split(' ');
            name = nameParts[^1] + " " + string.Join(' ', nameParts[..^1]);
        }

        return name;
    }

    private async Task<string?> GetNameFromRegister(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        const string apiUrl = "register/api/v1/dialogporten/parties/query";

        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
        {
            return null;
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
