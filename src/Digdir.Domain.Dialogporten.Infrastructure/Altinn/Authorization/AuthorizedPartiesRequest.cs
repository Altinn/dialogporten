using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class AuthorizedPartiesRequest(
    IPartyIdentifier partyIdentifier,
    bool includeAccessPackages = false,
    bool includeRoles = false,
    bool includeResources = false,
    bool includeInstances = false,
    IEnumerable<AuthorizedPartyFilter>? partyFilter = null)
{
    public IPartyIdentifier PartyIdentifier => partyIdentifier;

    [JsonPropertyName("partyFilter")]
    public List<AuthorizedPartyFilter> PartyFilter { get; } =
        partyFilter?.Select(filter => new AuthorizedPartyFilter
        {
            Type = filter.Type,
            Value = filter.Value
        }).ToList() ?? [];

    [JsonIgnore]
    public bool IncludeAccessPackages { get; } = includeAccessPackages;

    [JsonIgnore]
    public bool IncludeRoles { get; } = includeRoles;

    [JsonIgnore]
    public bool IncludeResources { get; } = includeResources;

    [JsonIgnore]
    public bool IncludeInstances { get; } = includeInstances;
}

internal static class AuthorizedPartiesRequestExtensions
{
    public static string GenerateCacheKey(this AuthorizedPartiesRequest request)
    {
        var optionsKey =
            $"{BoolToChar(request.IncludeAccessPackages)}{BoolToChar(request.IncludeRoles)}" +
            $"{BoolToChar(request.IncludeResources)}{BoolToChar(request.IncludeInstances)}";

        var partyFilterKey = request.PartyFilter.Count == 0
            ? string.Empty
            : string.Join(";", request.PartyFilter
                .OrderBy(filter => filter.Type, StringComparer.Ordinal)
                .ThenBy(filter => filter.Value, StringComparer.Ordinal)
                .Select(filter => $"{filter.Type}:{filter.Value}"));

        var rawKey = $"{request.PartyIdentifier.FullId}|{optionsKey}|{partyFilterKey}";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return $"auth:parties:{hashString}";
    }

    private static char BoolToChar(bool value) => value ? '1' : '0';
}

internal sealed class AuthorizedPartyFilter
{
    public required string Type { get; init; }
    public required string Value { get; init; }
}
