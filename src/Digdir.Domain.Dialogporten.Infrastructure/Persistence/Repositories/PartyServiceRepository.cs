using System.Text.Json;
using Dapper;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

// Reads DialogParty/DialogServiceResource/DialogPartyServiceSummary to provide
// a cheap "which services exist for this party" lookup, with per-party cache entries.
internal sealed class PartyServiceRepository(
    NpgsqlDataSource dataSource,
    IFusionCacheProvider? cacheProvider = null) : IPartyServiceAssociationRepository
{
    private const string ServicePrefix = "urn:altinn:resource:";
    private const string CacheKeyPrefix = "ps:";

    private static readonly StringComparer Comparer = StringComparer.InvariantCultureIgnoreCase;

    private readonly NpgsqlDataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private readonly IFusionCache _cache =
        cacheProvider?.GetCache(nameof(IPartyServiceAssociationRepository))
        ?? new NullFusionCache(Options.Create(new FusionCacheOptions()));

    public async Task<Dictionary<string, HashSet<string>>> GetExistingServicesByParty(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> services,
        CancellationToken cancellationToken)
    {
        if (parties.Count == 0 || services.Count == 0)
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

        var requestedServices = services
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(Comparer);

        if (requestedServices.Count == 0)
        {
            return [];
        }

        var cachedServiceIdentifiersByParty = new Dictionary<string, HashSet<string>>(Comparer);
        var cacheMisses = new List<string>();

        foreach (var party in requestedParties)
        {
            var cached = await _cache.GetOrDefaultAsync<string[]>(GetCacheKey(party), token: cancellationToken);
            if (cached is null)
            {
                cacheMisses.Add(party);
                continue;
            }

            cachedServiceIdentifiersByParty[party] = cached.ToHashSet(Comparer);
        }

        if (cacheMisses.Count > 0)
        {
            var fetchedServiceIdentifiersByParty = await FetchServiceIdentifiersByParty(cacheMisses, cancellationToken);
            foreach (var party in cacheMisses)
            {
                if (!fetchedServiceIdentifiersByParty.TryGetValue(party, out var serviceIdentifiers))
                {
                    serviceIdentifiers = [];
                }

                cachedServiceIdentifiersByParty[party] = serviceIdentifiers;
                await _cache.SetAsync(GetCacheKey(party), serviceIdentifiers.ToArray(), token: cancellationToken);
            }
        }

        var result = new Dictionary<string, HashSet<string>>(Comparer);
        foreach (var party in requestedParties)
        {
            if (!cachedServiceIdentifiersByParty.TryGetValue(party, out var serviceIdentifiers))
            {
                continue;
            }

            var matchingServices = serviceIdentifiers
                .Select(x => $"{ServicePrefix}{x}")
                .Where(requestedServices.Contains)
                .ToHashSet(Comparer);

            if (matchingServices.Count > 0)
            {
                result[party] = matchingServices;
            }
        }

        return result;
    }

    private async Task<Dictionary<string, HashSet<string>>> FetchServiceIdentifiersByParty(
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

    private static string GetCacheKey(string party) => $"{CacheKeyPrefix}{party}";

    private sealed record CompactParty(char PartyType, string PartyIdentifier);

    private sealed record SummaryRow(char PartyType, string PartyIdentifier, string ServiceResourceIdentifier);
}
