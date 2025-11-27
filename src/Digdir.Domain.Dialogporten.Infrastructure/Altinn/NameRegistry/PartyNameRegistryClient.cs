using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal sealed class PartyNameRegistryClient : IPartyNameRegistry
{
    private readonly IFusionCache _cache;
    private readonly HttpClient _client;
    private readonly ILogger<PartyNameRegistryClient> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public PartyNameRegistryClient(HttpClient client, IFusionCacheProvider cacheProvider, ILogger<PartyNameRegistryClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cacheProvider.GetCache(nameof(NameRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
    }

    public async Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync<string?>(
            $"Name_{externalIdWithPrefix}",
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
    }

    public async Task<string?> GetOrgName(string org, CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync<string?>(
            $"OrgName_{org}",
            async (ctx, ct) =>
            {
                var name = await GetOrgNameFromRegister(org, ct);
                if (name is null)
                {
                    // Short negative cache
                    ctx.Options.Duration = TimeSpan.FromSeconds(10);
                }

                return name;
            },
            token: cancellationToken);
    }

    private async Task<string?> GetOrgNameFromRegister(string org, CancellationToken cancellationToken)
    {
        NameLookup nameLookup = new() { Parties = [new() { OrgNo = org }] };

        var name = await LookupName(nameLookup, cancellationToken);

        if (name is null)
        {
            // This is PII, but this is an error condition (probably due to missing Altinn profile)
            _logger.LogError("Failed to get name from party name registry for organisation {Org}", org);
        }

        return name;
    }

    private async Task<string?> GetNameFromRegister(string externalIdWithPrefix, CancellationToken cancellationToken)
    {

        if (!TryParse(externalIdWithPrefix, out var nameLookup))
        {
            return null;
        }

        var name = await LookupName(nameLookup, cancellationToken);

        if (name is null)
        {
            // This is PII, but this is an error condition (probably due to missing Altinn profile)
            _logger.LogError("Failed to get name from party name registry for external id {ExternalId}", externalIdWithPrefix);
        }

        return name;
    }
    private async Task<string?> LookupName(NameLookup nameLookup, CancellationToken cancellationToken)
    {

        const string apiUrl = "register/api/v1/parties/nameslookup";

        var nameLookupResult = await _client.PostAsJsonEnsuredAsync<NameLookupResult>(
            apiUrl,
            nameLookup,
            serializerOptions: SerializerOptions,
            cancellationToken: cancellationToken);

        var name = nameLookupResult.PartyNames.FirstOrDefault()?.Name;
        return name;
    }

    private static bool TryParse(string externalIdWithPrefix, [NotNullWhen(true)] out NameLookup? nameLookup)
    {
        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
        {
            nameLookup = null;
            return false;
        }

        nameLookup = partyIdentifier switch
        {
            NorwegianPersonIdentifier personIdentifier => new() { Parties = [new() { Ssn = personIdentifier.Id }] },
            NorwegianOrganizationIdentifier organizationIdentifier => new() { Parties = [new() { OrgNo = organizationIdentifier.Id }] },
            SystemUserIdentifier systemUserIdentifier => new() { Parties = [new() { OrgNo = systemUserIdentifier.Id }] },
            _ => null
        };

        return nameLookup is not null;
    }

    private sealed class NameLookup
    {
        public List<NameLookupParty> Parties { get; set; } = null!;
    }

    private sealed class NameLookupResult
    {
        public List<NameLookupParty> PartyNames { get; set; } = null!;
    }

    private sealed class NameLookupParty
    {
        public string Ssn { get; set; } = null!;
        public string OrgNo { get; set; } = null!;
        public string? Name { get; set; }
    }
}

internal sealed class LocalPartNameRegistryClient : IPartyNameRegistry
{
    public Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken) => Task.FromResult<string?>("Gunnar Gunnarson");
    public Task<string?> GetOrgName(string orgId, CancellationToken cancellationToken) => Task.FromResult<string?>("Gunnar Org");
}
