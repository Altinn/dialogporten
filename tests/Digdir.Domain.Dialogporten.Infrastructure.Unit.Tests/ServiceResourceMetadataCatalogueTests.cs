using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Infrastructure.ServiceResourceMetadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class ServiceResourceMetadataCatalogueTests
{
    private const string ResourceUrn = "urn:altinn:resource:some-service";

    [Fact]
    public async Task Builds_Once_And_Caches_All_Language_Items_Keyed_By_Full_Urn()
    {
        var builder = new CountingItemBuilder();
        // The catalogue resolves its build dependencies from a fresh DI scope (so a background/eager refresh does
        // not reuse a disposed request-scoped DbContext), so register the stubs in a real provider and hand it the
        // scope factory.
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceResourceMetadataItemBuilder>(builder)
            .AddSingleton<IPartyResourceReferenceRepository>(new StubReferencedResourcesRepository([ResourceUrn]))
            .BuildServiceProvider();
        var catalogue = new ServiceResourceMetadataCatalogue(
            CreateCacheProvider(), serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var first = await catalogue.GetEntries(TestContext.Current.CancellationToken);
        var second = await catalogue.GetEntries(TestContext.Current.CancellationToken);

        // The expensive build runs once regardless of how many times the catalogue is requested (churn removed).
        builder.CallCount.Should().Be(1);
        // The catalogue is built all-language (acceptedLanguages == null), so per-request pruning can happen later.
        builder.LastAcceptedLanguages.Should().BeNull();
        // Entries are keyed by full URN (matching the authorized-set comparison in the handler).
        first.Should().ContainSingle().Which.ResourceUrn.Should().Be(ResourceUrn);
        second.Should().BeSameAs(first);
    }

    private static IFusionCacheProvider CreateCacheProvider() =>
        TestFusionCache.CreateProvider(ServiceResourceMetadataCatalogue.CacheName);

    private sealed class CountingItemBuilder : IServiceResourceMetadataItemBuilder
    {
        public int CallCount { get; private set; }
        public List<AcceptedLanguage>? LastAcceptedLanguages { get; private set; }

        public Task<List<ServiceResourceMetadataItemDto>> BuildItems(
            IReadOnlyCollection<string> serviceResources,
            List<AcceptedLanguage>? acceptedLanguages,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastAcceptedLanguages = acceptedLanguages;
            var items = serviceResources
                .Select(urn => new ServiceResourceMetadataItemDto
                {
                    ServiceResource = new ServiceResourceMetadataServiceResourceDto
                    {
                        // The builder returns the stripped id; the catalogue re-prefixes to the full URN.
                        Id = urn["urn:altinn:resource:".Length..],
                        ResourceType = "GenericAccessResource",
                        Status = "Active",
                        IsDelegable = true,
                        MinimumAuthenticationLevel = 3,
                        Name = [new LocalizationDto { LanguageCode = "nb", Value = "Tjeneste" }],
                        Links = new LinkDto { Metadata = "https://example/meta" }
                    },
                    ServiceOwner = new ServiceResourceMetadataServiceOwnerDto
                    {
                        OrgNumber = "111111111",
                        Code = "ORG",
                        Name = [new LocalizationDto { LanguageCode = "nb", Value = "Etat" }]
                    }
                })
                .ToList();
            return Task.FromResult(items);
        }
    }

    private sealed class StubReferencedResourcesRepository(IReadOnlyCollection<string> referenced)
        : IPartyResourceReferenceRepository
    {
        public Task<IReadOnlyCollection<string>> GetReferencedResources(CancellationToken cancellationToken) =>
            Task.FromResult(referenced);

        public Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
            IReadOnlyCollection<string> parties,
            IReadOnlyCollection<string> resources,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
