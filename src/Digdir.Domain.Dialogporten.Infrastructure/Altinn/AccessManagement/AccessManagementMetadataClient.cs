using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.AccessManagement;

internal sealed class AccessManagementMetadataClient : IAccessManagementMetadata
{
    private const string RolesEndpoint = "accessmanagement/api/v1/meta/info/roles";
    private const string AccessPackagesEndpoint = "accessmanagement/api/v1/meta/info/accesspackages/export";
    public const string CacheName = "AccessManagementMetadata";
    private const string CacheKeyPrefix = "access-management-metadata:";
    private const string MetadataCacheKey = CacheKeyPrefix + "assembled";

    private static readonly string[] SupportedLanguages = ["nb", "en", "nn"];

    private readonly HttpClient _client;
    private readonly IFusionCache _cache;
    private readonly IMetadataLinkProvider _metadataLinkProvider;

    public AccessManagementMetadataClient(
        HttpClient client,
        IFusionCacheProvider cacheProvider,
        IMetadataLinkProvider metadataLinkProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(metadataLinkProvider);

        var cache = cacheProvider.GetCache(CacheName);
        ArgumentNullException.ThrowIfNull(cache);

        _client = client;
        _cache = cache;
        _metadataLinkProvider = metadataLinkProvider;
    }

    public async Task<AccessManagementMetadata> GetMetadata(CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync(
            MetadataCacheKey,
            async token =>
            {
                var localizedMetadata = await Task.WhenAll(SupportedLanguages
                    .Select(language => GetLocalizedMetadata(language, token)));

                var roles = localizedMetadata
                    .SelectMany(x => x.Roles)
                    .GroupBy(x => x.Subject, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x =>
                        {
                            var first = x.First();
                            var names = x
                                .Select(y => y.Name)
                                .Where(y => !string.IsNullOrWhiteSpace(y.Value))
                                .DistinctBy(y => y.LanguageCode, StringComparer.Ordinal)
                                .ToList();
                            return new AccessManagementRoleMetadata(
                                first.Id,
                                first.Urn,
                                names,
                                new LinkDto { Metadata = _metadataLinkProvider.GetRoleMetadataLink(first.Id) });
                        },
                        StringComparer.OrdinalIgnoreCase);

                var accessPackages = localizedMetadata
                    .SelectMany(x => x.AccessPackages)
                    .GroupBy(x => x.Urn, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x =>
                        {
                            var first = x.First();
                            var names = x
                                .Select(y => y.Name)
                                .Where(y => !string.IsNullOrWhiteSpace(y.Value))
                                .DistinctBy(y => y.LanguageCode, StringComparer.Ordinal)
                                .ToList();
                            return new AccessManagementAccessPackageMetadata(
                                first.Urn,
                                names,
                                new LinkDto { Metadata = _metadataLinkProvider.GetAccessPackageMetadataLink(first.Urn) });
                        },
                        StringComparer.OrdinalIgnoreCase);

                return new AccessManagementMetadata(roles, accessPackages);
            },
            token: cancellationToken);

    private async Task<LocalizedAccessManagementMetadata> GetLocalizedMetadata(
        string language,
        CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync(
            $"{CacheKeyPrefix}{language}",
            async token =>
            {
                var rolesTask = _client.GetFromJsonEnsuredAsync<List<RoleResponse>>(
                    RolesEndpoint,
                    headers => headers.AcceptLanguage.ParseAdd(language),
                    token);
                var accessPackagesTask = _client.GetFromJsonEnsuredAsync<List<AreaGroupResponse>>(
                    AccessPackagesEndpoint,
                    headers => headers.AcceptLanguage.ParseAdd(language),
                    token);

                await Task.WhenAll(rolesTask, accessPackagesTask);

                return new LocalizedAccessManagementMetadata(
                    [.. rolesTask.Result.SelectMany(x => ToLocalizedRoles(x, language))],
                    [.. accessPackagesTask.Result
                        .SelectMany(FlattenPackages)
                        .Where(x => !string.IsNullOrWhiteSpace(x.Urn))
                        .Select(x => new LocalizedAccessPackage(
                            x.Urn!,
                            new LocalizationDto
                            {
                                LanguageCode = language,
                                Value = x.Name ?? x.Urn!
                            }))]);
            },
            token: cancellationToken);

    private static IEnumerable<LocalizedRole> ToLocalizedRoles(RoleResponse role, string language)
    {
        if (role.Id is null)
        {
            yield break;
        }

        var urn = role.LegacyUrn ?? role.Urn;
        if (string.IsNullOrWhiteSpace(urn))
        {
            yield break;
        }

        var roleMetadata = new LocalizedRole(
            role.Id.Value,
            urn,
            string.Empty,
            new LocalizationDto
            {
                LanguageCode = language,
                Value = role.Name ?? urn
            });

        if (!string.IsNullOrWhiteSpace(role.LegacyUrn))
        {
            yield return roleMetadata with { Subject = role.LegacyUrn };
        }

        if (!string.IsNullOrWhiteSpace(role.Urn))
        {
            yield return roleMetadata with { Subject = role.Urn };
        }

        if (!string.IsNullOrWhiteSpace(role.LegacyRoleCode))
        {
            yield return roleMetadata with
            {
                Subject = $"{AltinnAuthorizationConstants.RolePrefix}{role.LegacyRoleCode}".ToLowerInvariant()
            };
        }
    }

    private static IEnumerable<PackageResponse> FlattenPackages(AreaGroupResponse group)
    {
        foreach (var area in group.Areas ?? [])
        {
            foreach (var package in area.Packages ?? [])
            {
                yield return package;
            }
        }
    }

    private sealed record LocalizedAccessManagementMetadata(
        List<LocalizedRole> Roles,
        List<LocalizedAccessPackage> AccessPackages);

    private sealed record LocalizedRole(
        Guid Id,
        string Urn,
        string Subject,
        LocalizationDto Name);

    private sealed record LocalizedAccessPackage(
        string Urn,
        LocalizationDto Name);

    private sealed class RoleResponse
    {
        public Guid? Id { get; init; }
        public string? Name { get; init; }
        public string? Urn { get; init; }
        public string? LegacyRoleCode { get; init; }
        public string? LegacyUrn { get; init; }
    }

    private sealed class AreaGroupResponse
    {
        public List<AreaResponse>? Areas { get; init; }
    }

    private sealed class AreaResponse
    {
        public List<PackageResponse>? Packages { get; init; }
    }

    private sealed class PackageResponse
    {
        public string? Name { get; init; }
        public string? Urn { get; init; }
    }
}
