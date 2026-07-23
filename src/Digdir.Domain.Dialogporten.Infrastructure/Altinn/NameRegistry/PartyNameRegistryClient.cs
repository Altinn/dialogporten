using System.Net.Http.Json;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal sealed class PartyNameRegistryClient : IPartyNameRegistry
{
    private readonly IFusionCache _cache;
    private readonly ILogger<PartyNameRegistryClient> _logger;
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly IPartyNameRegistryTransport _partyNameRegistryTransport;
    private bool _useCorrectPersonNameOrdering;

    public PartyNameRegistryClient(
        IFusionCacheProvider cacheProvider,
        ILogger<PartyNameRegistryClient> logger,
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        IPartyNameRegistryTransport partyNameRegistryTransport)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(applicationSettings);

        var cache = cacheProvider.GetCache(nameof(NameRegistry));
        ArgumentNullException.ThrowIfNull(cache);

        _logger = logger;
        _cache = cache;
        _applicationSettings = applicationSettings;
        _partyNameRegistryTransport = partyNameRegistryTransport;
    }

    public async Task<string> GetNameOrFail(string externalIdWithPrefix, CancellationToken ct)
    {
        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
            throw new ArgumentException($"Unable to parse PartyIdentifier {externalIdWithPrefix}");

        return TryGetLocalName(partyIdentifier)
            ?? await GetNameFromRegisterOrFail(partyIdentifier, ToNameLookup(partyIdentifier), ct);
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
            if (name is not null) return name;

            ctx.Options.SkipMemoryCacheWrite = true;
            ctx.Options.SkipDistributedCacheWrite = true;

            return null;
        };
    }

    public void CacheName(string actorId, string name)
    {
        if (PartyIdentifier.TryParse(actorId, out var partyIdentifier))
        {
            _cache.Set(GetCacheKey(actorId), FlipNameIfPerson(partyIdentifier, name));
        }
    }

    private string GetCacheKey(string externalIdWithPrefix)
    {
        // Use a instance member to ensure we use the same value in the factory method
        _useCorrectPersonNameOrdering = _applicationSettings.Value.FeatureToggle.UseCorrectPersonNameOrdering;
        return $"Name{(_useCorrectPersonNameOrdering ? "_v2" : "")}_{externalIdWithPrefix}";
    }

    private async Task<string?> GetNameFromRegister(string externalIdWithPrefix, CancellationToken ct)
    {
        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
            throw new ArgumentException($"Unable to parse PartyIdentifier {externalIdWithPrefix}");

        return TryGetLocalName(partyIdentifier)
            ?? await GetNameFromRegister(partyIdentifier, ToNameLookup(partyIdentifier), ct);
    }

    private static string? TryGetLocalName(IPartyIdentifier partyIdentifier) => partyIdentifier switch
    {
        AltinnSelfIdentifiedUserIdentifier x => x.Id,
        IdportenEmailUserIdentifier x => x.Id,
        FeideUserIdentifier x => $"Feide User ({x.Id[..6]})",
        NorwegianPersonIdentifier => null,
        NorwegianOrganizationIdentifier => null,
        SystemUserIdentifier => null,
        _ => throw new ArgumentOutOfRangeException()
    };

    private static NameLookup ToNameLookup(IPartyIdentifier partyIdentifier) => partyIdentifier switch
    {
        NorwegianPersonIdentifier => new NameLookup { Data = [partyIdentifier.FullId] },
        NorwegianOrganizationIdentifier => new NameLookup { Data = [partyIdentifier.FullId] },
        SystemUserIdentifier => new NameLookup { Data = [partyIdentifier.FullId] },
        _ => throw new ArgumentOutOfRangeException()
    };

    private async Task<string?> GetNameFromRegister(IPartyIdentifier partyIdentifier, NameLookup nameLookup,
        CancellationToken ct)
    {
        var nameLookupResult = await PerformPartyNameRequest(nameLookup, ct);
        if (nameLookupResult is null) return null;

        var name = ProcessPartyNameResponse(partyIdentifier, nameLookupResult);

        if (name is not null) return name;

        _logger.LogWarning(
            "Search in party name registry returned no results for external id {ExternalId}. Response: {@Response}",
            partyIdentifier.FullId,
            nameLookupResult
        );

        return null;
    }

    private async Task<string> GetNameFromRegisterOrFail(IPartyIdentifier partyIdentifier, NameLookup nameLookup, CancellationToken ct)
    {
        var nameLookupResult = await PerformPartyNameRequestOrFail(nameLookup, ct);
        var name = ProcessPartyNameResponse(partyIdentifier, nameLookupResult);

        if (name is not null) return name;

        _logger.LogError(
            "Search in party name registry returned no results for external id {ExternalId}. Response: {@Response}",
            partyIdentifier.FullId,
            nameLookupResult
        );

        throw new InvalidOperationException("Search in party name registry returned no results");

    }

    private string? ProcessPartyNameResponse(
        IPartyIdentifier partyIdentifier,
        NameLookupResult nameLookupResult
    )
    {
        var name = nameLookupResult.Data.FirstOrDefault()?.DisplayName;

        // TODO! Currently, arbeidsflate expects the name ordering to be "Last First" for Norwegian persons, and does
        // the flip itself for persons. See https://github.com/Altinn/dialogporten/issues/3171
        return !string.IsNullOrWhiteSpace(name) ? FlipNameIfPerson(partyIdentifier, name) : null;
    }

    private async Task<NameLookupResult> PerformPartyNameRequestOrFail(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var response = await _partyNameRegistryTransport.QueryPartyName(nameLookup, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed POST {ApiUrl} with: {RequestBody}. Status code: {StatusCode}. ResponseBody: {ResponseBody}",
                PartyNameRegistryTransport.QueryPartiesUrl,
                nameLookup,
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

            throw new HttpRequestException($"Failed to POST {PartyNameRegistryTransport.QueryPartiesUrl}");
        }

        return await response.Content.ReadFromJsonAsync<NameLookupResult>(cancellationToken) ??
                      throw new JsonException($"Failed to deserialize JSON to type {typeof(NameLookupResult).FullName} from {PartyNameRegistryTransport.QueryPartiesUrl}");
    }

    private async Task<NameLookupResult?> PerformPartyNameRequest(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var response = await _partyNameRegistryTransport.QueryPartyName(nameLookup, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed POST {ApiUrl} with: {RequestBody}. Status code: {StatusCode}. ResponseBody: {ResponseBody}",
                PartyNameRegistryTransport.QueryPartiesUrl,
                nameLookup,
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

            return null;
        }

        return await response.Content.ReadFromJsonAsync<NameLookupResult>(cancellationToken) ??
                      throw new JsonException($"Failed to deserialize JSON to type {typeof(NameLookupResult).FullName} from {PartyNameRegistryTransport.QueryPartiesUrl}");
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
}
