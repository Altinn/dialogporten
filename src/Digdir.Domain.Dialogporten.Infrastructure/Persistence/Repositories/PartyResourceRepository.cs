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
/// Resolves existing <c>party x service resource</c> references from summary tables and caches
/// service sets per party using FusionCache.
///
/// Input parties and services resources are expected as full URNs. Internally, parties are compacted
/// to type prefix + identifier and service resource are handled unprefixed.
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
        if (parties.Count == 0 || resources.Count == 0)
        {
            return [];
        }

        var requestedParties = parties
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(Comparer)
            .ToList();

        if (requestedParties.Count == 0)
        {
            return [];
        }

        var requestedResources = resources
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(Comparer);

        if (requestedResources.Count == 0)
        {
            return [];
        }

        var cachedResourcesByParty = new Dictionary<string, HashSet<string>>(Comparer);
        var cacheMisses = new List<string>();

        foreach (var party in requestedParties)
        {
            var cached = await _cache.GetOrDefaultAsync<string[]>(GetCacheKey(party), token: cancellationToken);
            if (cached is null)
            {
                cacheMisses.Add(party);
                continue;
            }

            cachedResourcesByParty[party] = cached.ToHashSet(Comparer);
        }

        if (cacheMisses.Count > 0)
        {
            var fetchedResourcesByParty = await FetchResourcesByParty(cacheMisses, cancellationToken);
            foreach (var party in cacheMisses)
            {
                if (!fetchedResourcesByParty.TryGetValue(party, out var resourceIds))
                {
                    resourceIds = [];
                }

                cachedResourcesByParty[party] = resourceIds;
                await _cache.SetAsync(GetCacheKey(party), resourceIds.ToArray(), token: cancellationToken);
            }
        }

        var result = new Dictionary<string, HashSet<string>>(Comparer);
        foreach (var party in requestedParties)
        {
            if (!cachedResourcesByParty.TryGetValue(party, out var resourceIds))
            {
                continue;
            }

            var matchingServices = resourceIds
                .Select(x => $"{ResourcePrefix}{x}")
                .Where(requestedResources.Contains)
                .ToHashSet(Comparer);

            if (matchingServices.Count > 0)
            {
                result[party] = matchingServices;
            }
        }

        return result;
    }

    public async Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken)
    {
        // This uses the backplane to invalidate the cache across replicas.
        await _cache.ExpireAsync(GetCacheKey(party), token: cancellationToken);
    }

    private async Task<Dictionary<string, HashSet<string>>> FetchResourcesByParty(
        List<string> parties,
        CancellationToken cancellationToken)
    {
        var compactParties = parties
            .Select(ToCompactParty)
            .Distinct()
            .ToList();

        if (compactParties.Count == 0)
        {
            return [];
        }

        const string sql =
            """
            WITH input_parties AS (
                SELECT x."PartyType"
                     , x."PartyIdentifier"
                FROM jsonb_to_recordset(@Parties::jsonb)
                    AS x("PartyType" char(1), "PartyIdentifier" text)
            )
            SELECT dp."PartyType" AS "PartyType"
                 , dp."PartyIdentifier" AS "PartyIdentifier"
                 , dsr."ServiceResourceIdentifier" AS "ServiceResourceIdentifier"
            FROM input_parties ip
            JOIN "DialogParty" dp
              ON dp."PartyType" = ip."PartyType"
             AND dp."PartyIdentifier" = ip."PartyIdentifier"
            JOIN "DialogPartyServiceSummary" dpss
              ON dpss."PartyId" = dp."Id"
            JOIN "DialogServiceResource" dsr
              ON dsr."Id" = dpss."ServiceResourceId"
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Parties = JsonSerializer.Serialize(compactParties)
            },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<SummaryRow>(command);

        return rows
            .GroupBy(
                x => ToPartyUrn(x.PartyType, x.PartyIdentifier),
                Comparer)
            .ToDictionary(
                x => x.Key,
                x => x
                    .Select(y => y.ServiceResourceIdentifier)
                    .ToHashSet(Comparer),
                Comparer);
    }

    private static CompactParty ToCompactParty(string party) =>
        !PartyIdentifier.TryParse(party.AsSpan(), out var partyIdentifier)
        || !PartyIdentifier.TryGetShortPrefix(partyIdentifier, out var shortPrefix)
            ? throw new InvalidOperationException($"Unsupported party URN format: {party}")
            : new CompactParty(shortPrefix, partyIdentifier.Id);

    private static string ToPartyUrn(char partyType, string partyIdentifier) =>
        !PartyIdentifier.TryGetPrefixWithSeparator(partyType, out var prefixWithSeparator)
            ? throw new InvalidOperationException($"Unsupported party type '{partyType}'.")
            : $"{prefixWithSeparator}{partyIdentifier}";

    private static string GetCacheKey(string party)
    {
        var hashedParty = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(party)));
        return $"{CacheKeyPrefix}{hashedParty}";
    }

    private sealed record CompactParty(char PartyType, string PartyIdentifier);

    private sealed record SummaryRow(char PartyType, string PartyIdentifier, string ServiceResourceIdentifier);
}
