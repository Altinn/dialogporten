using Digdir.Domain.Dialogporten.Application.Externals;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.OrganizationRegistry;

internal sealed class ServiceOwnerNameRegistryClient : IServiceOwnerNameRegistry
{
    private const string ServiceOwnerShortNameReferenceCacheKey = "ServiceOwnerShortNameReference";

    private readonly IFusionCache _cache;
    private readonly HttpClient _client;

    public ServiceOwnerNameRegistryClient(HttpClient client, IFusionCacheProvider cacheProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _cache = cacheProvider.GetCache(nameof(OrganizationRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
    }

    public async Task<ServiceOwnerInfo?> GetServiceOwnerInfo(string orgNumber, CancellationToken cancellationToken)
    {
        var orgInfoByOrgNumber = await _cache.GetOrSetAsync(ServiceOwnerShortNameReferenceCacheKey, GetServiceOwnerInfo, token: cancellationToken);
        orgInfoByOrgNumber.TryGetValue(orgNumber, out var orgInfo);

        return orgInfo;
    }

    private async Task<Dictionary<string, ServiceOwnerInfo>> GetServiceOwnerInfo(CancellationToken cancellationToken)
    {
        const string searchEndpoint = "orgs/altinn-orgs.json";

        var response = await _client
            .GetFromJsonEnsuredAsync<OrganizationRegistryResponse>(searchEndpoint, cancellationToken: cancellationToken);

        var serviceOwnerInfoByOrgNumber = response
            .Orgs
            .ToDictionary(pair => pair.Value.Orgnr, pair => new ServiceOwnerInfo
            {
                OrgNumber = pair.Value.Orgnr,
                ShortName = pair.Key,
                DisplayName = pair.Value.Name?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => new ResourceLocalization(x.Key.Trim().ToLowerInvariant(), x.Value))
                    .ToList() ?? []
            });

        return serviceOwnerInfoByOrgNumber;
    }

    private sealed class OrganizationRegistryResponse
    {
        public required IDictionary<string, OrganizationDetails> Orgs { get; init; }
    }

    private sealed class OrganizationDetails
    {
        public IDictionary<string, string>? Name { get; init; }
        public string? Logo { get; init; }
        public required string Orgnr { get; init; }
        public string? Homepage { get; init; }
        public IList<string>? Environments { get; init; }
    }
}
