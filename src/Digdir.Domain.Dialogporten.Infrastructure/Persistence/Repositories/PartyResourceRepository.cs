using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

/// <summary>
/// Resolves existing <c>party x resource</c> references from dedicated partyresource tables and caches
/// resource sets per party using FusionCache.
///
/// Input parties and resources are expected as full URNs. Internally, parties and resources are handled
/// in unprefixed form together with a separate short party prefix.
/// </summary>
internal sealed class PartyResourceRepository(
    NpgsqlDataSource dataSource,
    IFusionCacheProvider? cacheProvider = null) : IPartyResourceReferenceRepository
{
    private const string ResourcePrefix = "urn:altinn:resource:";
    private const string CacheKeyPrefix = "ps:";

    private static readonly StringComparer Comparer = StringComparer.InvariantCultureIgnoreCase;

    private readonly NpgsqlDataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

    private readonly IFusionCache _cache =
        cacheProvider?.GetCache(nameof(IPartyResourceReferenceRepository))
        ?? new NullFusionCache(Options.Create(new FusionCacheOptions()));

    public async Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> resources,
        CancellationToken cancellationToken)
    {
        var request = TryNormalizeRequest(parties, resources);
        if (request is null)
        {
            return [];
        }

        var cacheLookup = await LoadCachedResourcesByParty(request.Parties, cancellationToken);
        await PopulateCacheMisses(cacheLookup, cancellationToken);

        return BuildResult(
            request.Parties,
            request.Resources,
            cacheLookup.CachedResourcesByParty);
    }

    public async Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken) =>
        // This uses the backplane to invalidate the cache across replicas.
        await _cache.ExpireAsync(GetCacheKey(party), token: cancellationToken);

    private async Task<Dictionary<string, HashSet<string>>> FetchResourcesByParty(
        List<string> parties,
        CancellationToken cancellationToken)
    {
        var unprefixedParties = parties
            .Select(ToUnprefixedParty)
            .Distinct()
            .ToList();

        if (unprefixedParties.Count == 0)
        {
            return [];
        }

        const string sql =
            """
            WITH input_parties AS (
                SELECT x."ShortPrefix"
                     , x."UnprefixedPartyIdentifier"
                FROM jsonb_to_recordset(@Parties::jsonb)
                    AS x("ShortPrefix" char(1), "UnprefixedPartyIdentifier" text)
            )
            SELECT p."ShortPrefix" AS "ShortPrefix"
                 , p."UnprefixedPartyIdentifier" AS "UnprefixedPartyIdentifier"
                 , r."UnprefixedResourceIdentifier" AS "UnprefixedResourceIdentifier"
            FROM input_parties ip
            JOIN partyresource."Party" p
              ON p."ShortPrefix" = ip."ShortPrefix"
             AND p."UnprefixedPartyIdentifier" = ip."UnprefixedPartyIdentifier"
            JOIN partyresource."PartyResource" pr
              ON pr."PartyId" = p."Id"
            JOIN partyresource."Resource" r
              ON r."Id" = pr."ResourceId"
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Parties = JsonSerializer.Serialize(unprefixedParties)
            },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<SummaryRow>(command);

        return rows
            .GroupBy(
                x => ToPartyUrn(x.ShortPrefix, x.UnprefixedPartyIdentifier),
                Comparer)
            .ToDictionary(
                x => x.Key,
                x => x
                    .Select(y => y.UnprefixedResourceIdentifier)
                    .ToHashSet(Comparer),
                Comparer);
    }

    private static NormalizedRequest? TryNormalizeRequest(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> resources)
    {
        if (parties.Count == 0 || resources.Count == 0)
        {
            return null;
        }

        var requestedParties = parties
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(Comparer)
            .ToList();

        if (requestedParties.Count == 0)
        {
            return null;
        }

        var requestedResources = resources
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(Comparer);

        return requestedResources.Count == 0 ? null : new NormalizedRequest(requestedParties, requestedResources);
    }

    private async Task<CacheLookup> LoadCachedResourcesByParty(
        List<string> requestedParties,
        CancellationToken cancellationToken)
    {
        var cachedResourcesByParty = new Dictionary<string, HashSet<string>>(Comparer);
        var cacheMisses = new List<string>();

        await foreach (var partyLookupAttempt in Task
                           .WhenEach(requestedParties.Select(TryGetCache))
                           .WithCancellation(cancellationToken))
        {
            var (party, cache) = await partyLookupAttempt;
            if (!cache.HasValue)
            {
                cacheMisses.Add(party);
                continue;
            }

            cachedResourcesByParty[party] = cache.Value.ToHashSet(Comparer);
        }

        return new CacheLookup(cachedResourcesByParty, cacheMisses);

        async Task<(string Key, MaybeValue<string[]> Value)> TryGetCache(string x)
        {
            var value = await _cache.TryGetAsync<string[]>(GetCacheKey(x), token: cancellationToken);
            return (x, value);
        }
    }

    private async Task PopulateCacheMisses(
        CacheLookup cacheLookup,
        CancellationToken cancellationToken)
    {
        if (cacheLookup.CacheMisses.Count == 0)
        {
            return;
        }

        var fetchedResourcesByParty = await FetchResourcesByParty(cacheLookup.CacheMisses, cancellationToken);
        var cacheTasks = new List<Task>();

        foreach (var party in cacheLookup.CacheMisses)
        {
            var resourceIds = cacheLookup.CachedResourcesByParty[party] =
                fetchedResourcesByParty.GetValueOrDefault(party) ?? [];
            cacheTasks.Add(
                _cache.SetAsync(GetCacheKey(party), resourceIds.ToArray(), token: cancellationToken).AsTask());
        }

        await Task.WhenAll(cacheTasks);
    }

    private static Dictionary<string, HashSet<string>> BuildResult(
        List<string> requestedParties,
        HashSet<string> requestedResources,
        Dictionary<string, HashSet<string>> resourcesByParty)
    {
        var result = new Dictionary<string, HashSet<string>>(Comparer);

        foreach (var party in requestedParties)
        {
            if (!resourcesByParty.TryGetValue(party, out var resourceIds))
            {
                continue;
            }

            var matchingResources = resourceIds
                .Select(x => $"{ResourcePrefix}{x}")
                .Where(requestedResources.Contains)
                .ToHashSet(Comparer);

            if (matchingResources.Count == 0)
            {
                continue;
            }

            result[party] = matchingResources;
        }

        return result;
    }

    private static UnprefixedParty ToUnprefixedParty(string party) =>
        !PartyIdentifier.TryParse(party.AsSpan(), out var partyIdentifier)
        || !PartyIdentifier.TryGetShortPrefix(partyIdentifier, out var shortPrefix)
            ? throw new InvalidOperationException($"Unsupported party URN format: {party}")
            : new UnprefixedParty(shortPrefix, partyIdentifier.Id);

    private static string ToPartyUrn(char shortPrefix, string unprefixedPartyIdentifier) =>
        !PartyIdentifier.TryGetPrefixWithSeparator(shortPrefix, out var prefixWithSeparator)
            ? throw new InvalidOperationException($"Unsupported short prefix '{shortPrefix}'.")
            : $"{prefixWithSeparator}{unprefixedPartyIdentifier}";

    private static string GetCacheKey(string party)
    {
        var hashedParty = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(party)));
        return $"{CacheKeyPrefix}{hashedParty}";
    }

    private sealed record NormalizedRequest(List<string> Parties, HashSet<string> Resources);

    private sealed record CacheLookup(
        Dictionary<string, HashSet<string>> CachedResourcesByParty,
        List<string> CacheMisses);

    private sealed record UnprefixedParty(char ShortPrefix, string UnprefixedPartyIdentifier);

    private sealed record SummaryRow(
        char ShortPrefix,
        string UnprefixedPartyIdentifier,
        string UnprefixedResourceIdentifier);
}
