using Digdir.Domain.Dialogporten.Application.Externals;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.OrganizationRegistry;

internal sealed class ServiceOwnerNameRegistryClient : IServiceOwnerNameRegistry
{
    private const string ServiceOwnerShortNameReferenceCacheKey = "ServiceOwnerShortNameReference_v2";

    private readonly IFusionCache _cache;
    private readonly HttpClient _client;

    public ServiceOwnerNameRegistryClient(HttpClient client, IFusionCacheProvider cacheProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cacheProvider);

        var cache = cacheProvider.GetCache(nameof(OrganizationRegistry));
        ArgumentNullException.ThrowIfNull(cache, nameof(cacheProvider));

        _client = client;
        _cache = cache;
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
                DisplayName = pair.Value.Name.ToLocalizations()
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
