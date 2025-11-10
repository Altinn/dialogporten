using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;

internal sealed class ResourceRegistryClient : IResourceRegistry
{
    private const string ServiceResourceInformationCacheKey = "ServiceResourceInformationCacheKey";
    private const string ResourceRegistryResourceEndpoint = "resourceregistry/api/v1/resource/";
    private const string AuthenticationLevelCategory = "urn:altinn:minimum-authenticationlevel";

    private readonly IFusionCache _cache;
    private readonly HttpClient _client;
    private readonly ILogger<ResourceRegistryClient> _logger;

    public ResourceRegistryClient(HttpClient client, IFusionCacheProvider cacheProvider, ILogger<ResourceRegistryClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _cache = cacheProvider.GetCache(nameof(ResourceRegistry)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<ServiceResourceInformation>> GetResourceInformationForOrg(
        string orgNumber,
        CancellationToken cancellationToken)
    {
        var resources = await FetchServiceResourceInformation(cancellationToken);
        return resources
            .Where(x => x.OwnerOrgNumber == orgNumber)
            .ToList();
    }

    public async Task<ServiceResourceInformation?> GetResourceInformation(
        string serviceResourceId,
        CancellationToken cancellationToken)
    {
        var resources = await FetchServiceResourceInformation(cancellationToken);
        return resources
            .FirstOrDefault(x => x.ResourceId == serviceResourceId);
    }

    public async IAsyncEnumerable<List<UpdatedSubjectResource>> GetUpdatedSubjectResources(DateTimeOffset since, int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string searchEndpoint = $"{ResourceRegistryResourceEndpoint}updated";
        var nextUrl = searchEndpoint + $"?since={Uri.EscapeDataString(since.ToString("O"))}&limit={batchSize}";

        do
        {
            var response = await _client
                .GetFromJsonEnsuredAsync<UpdatedResponse>(nextUrl,
                    cancellationToken: cancellationToken);

            if (response.Data.Count == 0)
            {
                yield break;
            }

            yield return response.Data;

            nextUrl = response.Links.Next?.ToString();
        } while (nextUrl is not null);
    }

    public async Task<IReadOnlyCollection<UpdatedResourcePolicyInformation>> GetUpdatedResourcePolicyInformation(DateTimeOffset since,
        int numberOfConcurrentRequests,
        CancellationToken cancellationToken)
    {
        // First fetch all unique updated resources serially using the supplied "since" value, then iterate them with a fan out to fetch and parse the policies
        // concurrently using semaphores.
        var updatedResources = await GetUniqueUpdatedResources(since, cancellationToken);

        using var semaphore = new SemaphoreSlim(numberOfConcurrentRequests);
        var metadataTasks = new List<Task<UpdatedResourcePolicyInformation?>>();

        foreach (var updatedResource in updatedResources)
        {
            metadataTasks.Add(ProcessResourcePolicy(updatedResource, semaphore, cancellationToken));
        }

        // Filter out null values (indicating missing/invalid policies)
        return (await Task.WhenAll(metadataTasks))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
    }

    private async Task<UpdatedResourcePolicyInformation?> ProcessResourcePolicy(UpdatedResource item, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await GetUpdatedResourcePolicyInformation(item, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<List<UpdatedResource>> GetUniqueUpdatedResources(DateTimeOffset _, CancellationToken cancellationToken)
    {
        // Until we have an API in RR to fetch updated resources only, we have to fetch them all
        return (await FetchServiceResourceInformation(cancellationToken))
            .Select(x => new UpdatedResource(new Uri(x.ResourceId), DateTimeOffset.MinValue))
            .ToList();
    }

    private async Task<UpdatedResourcePolicyInformation?> GetUpdatedResourcePolicyInformation(UpdatedResource resource, CancellationToken cancellationToken)
    {
        var resourceRegistryEntry = new ResourceRegistryEntry(resource.ResourceUrn);
        if (!resourceRegistryEntry.HasPolicyInResourceRegistry)
        {
            return null;
        }

        try
        {
            var minimumAuthenticationLevel = GetMinimumAuthenticationLevel(await FetchPolicy(resourceRegistryEntry.Identifier, cancellationToken));
            return minimumAuthenticationLevel is null
                ? null
                : new UpdatedResourcePolicyInformation(
                    resource.ResourceUrn,
                    minimumAuthenticationLevel.Value,
                    resource.UpdatedAt);
        }
        catch (Exception ex)
        {
            // We need to keep going here, so we log and return a default value
            _logger.LogWarning(ex, "Failed to process policy for \"{ResourceUrn}\"", resource.ResourceUrn);
            return null;
        }
    }

    private async Task<XacmlPolicy> FetchPolicy(string resourceIdentifier, CancellationToken cancellationToken)
    {
        const string policyEndpointPattern = ResourceRegistryResourceEndpoint + "{0}/policy";

        await using var policyXml = await _client
            .GetStreamAsync(string.Format(CultureInfo.InvariantCulture, policyEndpointPattern, resourceIdentifier),
                cancellationToken: cancellationToken);

        using var reader = XmlReader.Create(policyXml);
        return XacmlParser.ParseXacmlPolicy(reader);
    }

    private static int? GetMinimumAuthenticationLevel(XacmlPolicy policy)
    {
        var authenticationLevelAttributeAssignmentExpression = policy.ObligationExpressions
            .FirstOrDefault()?.AttributeAssignmentExpressions
            .FirstOrDefault(x => x.Category?.ToString() == AuthenticationLevelCategory);

        if (authenticationLevelAttributeAssignmentExpression?.Property is XacmlAttributeValue attributeValue)
        {
            if (int.TryParse(attributeValue.Value, out var minimumSecurityLevel))
            {
                return minimumSecurityLevel;
            }
        }

        // No minimum authentication level defined
        return null;
    }

    private async Task<ServiceResourceInformation[]> FetchServiceResourceInformation(CancellationToken cancellationToken)
    {
        const string searchEndpoint = $"{ResourceRegistryResourceEndpoint}resourcelist?includeMigratedApps=true";

        return await _cache.GetOrSetAsync(
            ServiceResourceInformationCacheKey,
            async cToken =>
            {
                var response = await _client
                    .GetFromJsonEnsuredAsync<List<ResourceListResponse>>(searchEndpoint,
                        cancellationToken: cToken);

                return response
                    .Where(x => !string.IsNullOrWhiteSpace(x.HasCompetentAuthority.Organization))
                    .Where(x => !string.IsNullOrWhiteSpace(x.HasCompetentAuthority.OrgCode))
                    .Where(x => Application.Common.Authorization.Constants.SupportedResourceTypes.Contains(x.ResourceType))
                    .Select(x => new ServiceResourceInformation(
                        $"{Constants.ServiceResourcePrefix}{x.Identifier}",
                        x.ResourceType,
                        x.HasCompetentAuthority.Organization!,
                        x.HasCompetentAuthority.OrgCode!))
                    .ToArray();
            },
            token: cancellationToken);
    }

    private sealed class ResourceRegistryEntry
    {
        public string Identifier { get; }
        public bool HasPolicyInResourceRegistry { get; }

        private const string Altinn2ServicePrefix = "se_";
        private const string AltinnAppPrefix = "app_";
        private const char UrnSeparator = ':';

        public ResourceRegistryEntry(Uri resourceUrn)
        {
            // Utility class to extract the identifier from a resource URN, and determine if it has a policy
            // available in the resource registry API (Altinn 2 representations and Altinn Apps do not)
            var fullIdentifier = resourceUrn.ToString();
            Identifier = fullIdentifier[(fullIdentifier.LastIndexOf(UrnSeparator) + 1)..];
            HasPolicyInResourceRegistry = !Identifier.StartsWith(Altinn2ServicePrefix, StringComparison.Ordinal)
                        && !Identifier.StartsWith(AltinnAppPrefix, StringComparison.Ordinal);
        }
    }

    private sealed class ResourceListResponse
    {
        public required string Identifier { get; init; }
        public required CompetentAuthority HasCompetentAuthority { get; init; }
        public required string ResourceType { get; init; }
    }

    private sealed class CompetentAuthority
    {
        // Altinn 2 resources do not always have an organization number as competent authority, only service owner code
        // We filter these out anyway, but we need to allow null here
        public string? Organization { get; init; }
        public string? OrgCode { get; init; }
    }

    private sealed record UpdatedResponse(UpdatedResponseLinks Links, List<UpdatedSubjectResource> Data);
    private sealed record UpdatedResponseLinks(Uri? Next);
    private sealed record UpdatedResource(Uri ResourceUrn, DateTimeOffset UpdatedAt);
}
