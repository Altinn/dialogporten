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

            var matchingResources = resourceIds
                .Select(x => $"{ResourcePrefix}{x}")
                .Where(requestedResources.Contains)
                .ToHashSet(Comparer);

            if (matchingResources.Count > 0)
            {
                result[party] = matchingResources;
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

    private sealed record UnprefixedParty(char ShortPrefix, string UnprefixedPartyIdentifier);

    private sealed record SummaryRow(char ShortPrefix, string UnprefixedPartyIdentifier, string UnprefixedResourceIdentifier);
}
