using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class SearchAuthorizedServiceResourcesQueryHandlerTests
{
    private const string ResourceA = "urn:altinn:resource:a";
    private const string ResourceB = "urn:altinn:resource:b";
    private const string UnreferencedResource = "urn:altinn:resource:not-in-catalogue";

    [Fact]
    public async Task Result_Is_Restricted_To_The_Referenced_Catalogue()
    {
        // Catalogue (referenced resources) = {A, B}. Authorized set = {A, X} where X is not referenced.
        // The result must contain only A: B is dropped (not authorized) and X is dropped (not in the catalogue),
        // so the response can never leak resources outside the referenced catalogue.
        var handler = new SearchAuthorizedServiceResourcesQueryHandler(
            new StubProvider(new AuthorizedServiceResources(IncludeFullCatalogue: false, [ResourceA, UnreferencedResource])),
            new StubCatalogue([ResourceA, ResourceB]));

        var result = await handler.Handle(new SearchAuthorizedServiceResourcesQuery(), TestContext.Current.CancellationToken);

        // Normal authorized result -> no fallback signal.
        result.IsFullCatalogueFallback.Should().BeNull();
        result.Items.Select(i => i.ServiceResource.Id).Should().Equal("a");
    }

    [Fact]
    public async Task Full_Catalogue_Fallback_Returns_Whole_Catalogue_And_Sets_The_Flag()
    {
        var handler = new SearchAuthorizedServiceResourcesQueryHandler(
            new StubProvider(new AuthorizedServiceResources(IncludeFullCatalogue: true, [])),
            new StubCatalogue([ResourceA, ResourceB]));

        var result = await handler.Handle(new SearchAuthorizedServiceResourcesQuery(), TestContext.Current.CancellationToken);

        result.IsFullCatalogueFallback.Should().BeTrue();
        result.Items.Select(i => i.ServiceResource.Id).Should().BeEquivalentTo("a", "b");
    }

    private sealed class StubProvider(AuthorizedServiceResources result) : IAuthorizedServiceResourcesProvider
    {
        public Task<AuthorizedServiceResources> GetAuthorizedServiceResources(string[]? partyFilter, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class StubCatalogue(IReadOnlyList<string> resourceUrns) : IServiceResourceMetadataCatalogue
    {
        public Task<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>> GetEntries(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ServiceResourceMetadataCatalogueEntry>>(
                resourceUrns.Select(urn => new ServiceResourceMetadataCatalogueEntry(urn, CreateItem(urn))).ToList());
    }

    private static ServiceResourceMetadataItemDto CreateItem(string fullUrn) => new()
    {
        ServiceResource = new ServiceResourceMetadataServiceResourceDto
        {
            Id = fullUrn["urn:altinn:resource:".Length..],
            ResourceType = "GenericAccessResource",
            Status = "Active",
            IsDelegable = true,
            MinimumAuthenticationLevel = 3,
            Name = [new LocalizationDto { LanguageCode = "nb", Value = fullUrn }],
            Links = new LinkDto { Metadata = "https://example/meta" }
        },
        Roles = [],
        AccessPackages = [],
        ServiceOwner = new ServiceResourceMetadataServiceOwnerDto { OrgNumber = "111111111", Code = "ORG", Name = [] }
    };
}
