﻿using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;

internal sealed class ResourceRegistryClient : IResourceRegistry
{
    private const string ServiceResourceInformationByOrgCacheKey = "ServiceResourceInformationByOrgCacheKey";
    private const string ServiceResourceInformationByResourceIdCacheKey = "ServiceResourceInformationByResourceIdCacheKey";
    private const string ResourceTypeGenericAccess = "GenericAccessResource";
    private const string ResourceTypeAltinnApp = "AltinnApp";
    private const string ResourceTypeCorrespondence = "Correspondence";

    private readonly IFusionCache _cache;
    private readonly HttpClient _client;

    public ResourceRegistryClient(HttpClient client, IFusionCacheProvider cacheProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _cache = cacheProvider.GetCache(nameof(ResourceRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
    }

    public async Task<IReadOnlyCollection<ServiceResourceInformation>> GetResourceInformationForOrg(
        string orgNumber,
        CancellationToken cancellationToken)
    {
        var dic = await GetOrSetResourceInformationByOrg(cancellationToken);
        if (!dic.TryGetValue(orgNumber, out var resources))
        {
            resources = [];
        }

        return resources.AsReadOnly();
    }

    public async Task<ServiceResourceInformation?> GetResourceInformation(
        string serviceResourceId,
        CancellationToken cancellationToken)
    {
        var dic = await GetOrSetResourceInformationByResourceId(cancellationToken);
        dic.TryGetValue(serviceResourceId, out var resource);
        return resource;
    }

    public async Task<List<UpdatedSubjectResource>> GetUpdatedSubjectResources(DateTimeOffset since, CancellationToken cancellationToken)
    {
        const string searchEndpoint = "resourceregistry/api/v1/resource/updated";
        var resources = new List<UpdatedSubjectResource>();
        var nextUrl = searchEndpoint + "?since=" + since.ToString("O");
        do
        {
            var response = await _client
                .GetFromJsonEnsuredAsync<UpdatedResponse>(nextUrl,
                    cancellationToken: cancellationToken);

            resources.AddRange(response.Data.Select(item =>
                new UpdatedSubjectResource
                {
                    Resource = item.ResourceUrn,
                    Subject = item.SubjectUrn,
                    UpdatedAt = item.UpdatedAt,
                    Deleted = item.Deleted
                }));

            nextUrl = response.Links.Next?.ToString();

        } while (nextUrl is not null);

        return resources;
    }

    private async Task<Dictionary<string, ServiceResourceInformation[]>> GetOrSetResourceInformationByOrg(
        CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            ServiceResourceInformationByOrgCacheKey,
            async cToken =>
            {
                var resources = await FetchServiceResourceInformation(cToken);
                return resources
                    .GroupBy(x => x.OwnerOrgNumber)
                    .ToDictionary(x => x.Key, x => x.ToArray());
            },
            token: cancellationToken);
    }

    private async Task<Dictionary<string, ServiceResourceInformation>> GetOrSetResourceInformationByResourceId(
        CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            ServiceResourceInformationByResourceIdCacheKey,
            async cToken =>
            {
                var resources = await FetchServiceResourceInformation(cToken);
                return resources.ToDictionary(x => x.ResourceId);
            },
            token: cancellationToken);
    }

    private async Task<ServiceResourceInformation[]> FetchServiceResourceInformation(CancellationToken cancellationToken)
    {
        const string searchEndpoint = "resourceregistry/api/v1/resource/resourcelist";

        var response = await _client
            .GetFromJsonEnsuredAsync<List<ResourceListResponse>>(searchEndpoint,
                cancellationToken: cancellationToken);

        return response
            .Where(x => !string.IsNullOrWhiteSpace(x.HasCompetentAuthority.Organization))
            .Where(x => x.ResourceType is
                ResourceTypeGenericAccess or
                ResourceTypeAltinnApp or
                ResourceTypeCorrespondence)
            .Select(x => new ServiceResourceInformation(
                $"{Constants.ServiceResourcePrefix}{x.Identifier}",
                x.ResourceType,
                x.HasCompetentAuthority.Organization!))
            .ToArray();
    }

    private sealed class ResourceListResponse
    {
        public required string Identifier { get; init; }
        public required CompetentAuthority HasCompetentAuthority { get; init; }
        public required string ResourceType { get; init; }
    }

    private sealed class CompetentAuthority
    {
        // Altinn 2 resources does not always have an organization number as competent authority, only service owner code
        // We filter these out anyway, but we need to allow null here
        public string? Organization { get; init; }
        public required string OrgCode { get; init; }
    }

    private sealed class UpdatedResponse
    {
        public required UpdatedResponseLinks Links { get; init; }
        public required List<UpdatedResponseItem> Data { get; init; }
    }

    private sealed class UpdatedResponseLinks
    {
        public Uri? Next { get; init; }
    }

    private sealed class UpdatedResponseItem
    {
        public required Uri SubjectUrn { get; init; }
        public required Uri ResourceUrn { get; init; }
        public required DateTimeOffset UpdatedAt { get; init; }
        public required bool Deleted { get; init; }
    }
}
