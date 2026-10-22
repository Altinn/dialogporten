using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using ZiggyCreatures.Caching.Fusion;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal sealed class PartyNameRegistryClient : IPartyNameRegistry
{
    private readonly IFusionCache _cache;
    private readonly ILogger<PartyNameRegistryClient> _logger;
    private readonly IPartyNameRegistryTransport _partyNameRegistryTransport;
    private readonly bool _useCorrectPersonNameOrdering;

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
        _partyNameRegistryTransport = partyNameRegistryTransport;
        _useCorrectPersonNameOrdering = applicationSettings.Value.FeatureToggle.UseCorrectPersonNameOrdering;
    }

    public async Task<GetPartyNameResult> GetName(string externalIdWithPrefix, CancellationToken cancellationToken)
    {
        if (!PartyIdentifier.TryParse(externalIdWithPrefix, out var partyIdentifier))
        {
            return new PartyNameUnsupported(externalIdWithPrefix, "Unable to parse party identifier");
        }

        if (TryGetNameForUnsupportedIdentifiers(partyIdentifier, out var name)) return new PartyNameFound(name);
        if (!TryCreateNameLookup(partyIdentifier, out var nameLookup))
        {
            return new PartyNameUnsupported(
                externalIdWithPrefix,
                $"Name lookup is not supported for identifier type {partyIdentifier.GetType().Name}");
        }

        try
        {
            var registryName = await _cache.GetOrSetAsync<string?>(
                GetCacheKey(externalIdWithPrefix),
                (ctx, ct) => GetNameCacheFactory(ctx, partyIdentifier, nameLookup, ct),
                token: cancellationToken
            );

            return registryName is null
                ? new PartyNameNotFound(externalIdWithPrefix)
                : new PartyNameFound(registryName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User cancelled the request, rethrow to stop immediately.
            throw;
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.BadRequest)
        {
            throw;
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutException or OperationCanceledException)
        {
            _logger.LogWarning(e, "Party name registry unavailable for external id {ExternalId}", externalIdWithPrefix);
            return new PartyNameUnavailable(externalIdWithPrefix, e);
        }
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
        return $"Name{(_useCorrectPersonNameOrdering ? "_v2" : "")}_{externalIdWithPrefix}";
    }

    private async Task<string?> GetNameCacheFactory(
        FusionCacheFactoryExecutionContext<string?> ctx,
        IPartyIdentifier partyIdentifier,
        NameLookup nameLookup,
        CancellationToken ct
    )
    {
        var nameLookupResult = await PerformPartyNameRequest(nameLookup, ct);
        var name = ProcessPartyNameResponse(partyIdentifier, nameLookupResult);

        if (name is null)
        {
            ctx.Options.SkipDistributedCacheWrite = true;
            ctx.Options.Duration = TimeSpan.FromSeconds(1);
        }
        return name;
    }

    private string? ProcessPartyNameResponse(
        IPartyIdentifier partyIdentifier,
        NameLookupResult nameLookupResult
    )
    {
        var data = nameLookupResult.Data ?? throw new UnreachableException($"data cant be null. Id: {partyIdentifier}");
        var name = data.FirstOrDefault()?.DisplayName;
        if (name is null) return null;

        // TODO! Currently, arbeidsflate expects the name ordering to be "Last First" for Norwegian persons, and does
        // the flip itself for persons. See https://github.com/Altinn/dialogporten/issues/3171
        return FlipNameIfPerson(partyIdentifier, name);
    }

    private static bool TryCreateNameLookup(
        IPartyIdentifier partyIdentifier,
        [NotNullWhen(true)] out NameLookup? nameLookup
    )
    {

        nameLookup = partyIdentifier switch
        {
            NorwegianPersonIdentifier personIdentifier => new() { Data = [personIdentifier.FullId] },
            NorwegianOrganizationIdentifier organizationIdentifier => new() { Data = [organizationIdentifier.FullId] },
            SystemUserIdentifier systemUserIdentifier => new() { Data = [systemUserIdentifier.FullId] },
            _ => null
        };

        return nameLookup is not null;
    }

    /// <summary>
    /// We don't have any information about self-identified or Feide users in the party name registry
    /// </summary>
    private static bool TryGetNameForUnsupportedIdentifiers(
        IPartyIdentifier partyIdentifier,
        [NotNullWhen(true)] out string? name
    )
    {
        name = partyIdentifier switch
        {
            AltinnSelfIdentifiedUserIdentifier x => x.Id,
            IdportenEmailUserIdentifier x => x.Id,
            FeideUserIdentifier x => $"Feide User ({x.Id[..6]})",
            _ => null
        };

        return name is not null;
    }

    private async Task<NameLookupResult> PerformPartyNameRequest(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        using var response = await _partyNameRegistryTransport.QueryPartyNameResponse(nameLookup, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response
                   .Content
                   .ReadFromJsonAsync<NameLookupResult>(cancellationToken)
               ?? throw new JsonException(
                   $"Failed to deserialize JSON to type {typeof(NameLookupResult).FullName} from {PartyNameRegistryTransport.QueryPartiesUrl}"
               );
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
