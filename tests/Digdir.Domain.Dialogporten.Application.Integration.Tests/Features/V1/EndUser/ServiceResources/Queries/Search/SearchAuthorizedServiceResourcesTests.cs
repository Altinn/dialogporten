using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.ServiceResources.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchAuthorizedServiceResourcesTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string PartyA = "urn:altinn:organization:identifier-no:111111111";
    private const string PartyB = "urn:altinn:organization:identifier-no:222222222";
    private const string ResourceA = "urn:altinn:resource:authsr-a";
    private const string ResourceB = "urn:altinn:resource:authsr-b";

    [Fact]
    public async Task Returns_Empty_When_No_Authorizations()
    {
        ConfigureAuthorizedResources(new());

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery(),
            TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_Union_Of_Authorized_Resources_Across_Parties()
    {
        await SeedReferencedResource(PartyA, ResourceA, seed: 1);
        await SeedReferencedResource(PartyB, ResourceB, seed: 2);

        ConfigureAuthorizedResources(new()
        {
            [PartyA] = [ResourceA],
            [PartyB] = [ResourceB]
        });

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery(),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.ServiceResource.Id)
            .Should().BeEquivalentTo("authsr-a", "authsr-b");
    }

    [Fact]
    public async Task Party_Filter_Narrows_Result()
    {
        await SeedReferencedResource(PartyA, ResourceA, seed: 1);
        await SeedReferencedResource(PartyB, ResourceB, seed: 2);

        ConfigureAuthorizedResources(new()
        {
            [PartyA] = [ResourceA],
            [PartyB] = [ResourceB]
        });

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery { Parties = [PartyA] },
            TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle()
            .Which.ServiceResource.Id.Should().Be("authsr-a");
    }

    [Fact]
    public async Task Unknown_Party_Filter_Is_Silently_Dropped()
    {
        await SeedReferencedResource(PartyA, ResourceA, seed: 1);

        ConfigureAuthorizedResources(new()
        {
            [PartyA] = [ResourceA]
        });

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery { Parties = ["urn:altinn:organization:identifier-no:999999999"] },
            TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Does_Not_Return_Resources_Not_Referenced_By_Dialogporten()
    {
        // No dialog seeded for ResourceA, so it is not in the referenced catalogue. Even though the caller is
        // authorized for a single resource (well below the pruning threshold), it must be pruned away.
        ConfigureAuthorizedResources(new()
        {
            [PartyA] = [ResourceA]
        });

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery(),
            TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Enriches_Authorized_Resource_With_Roles_And_AccessPackages()
    {
        const string serviceResource = "urn:altinn:resource:authsr-enriched";
        const string roleSubject = "urn:altinn:rolecode:DIALOG_READ";
        const string accessPackageSubject = "urn:altinn:accesspackage:dialog_lookup_package";

        // Seed a dialog for PartyA so the resource is referenced in the partyresource schema (and thus survives
        // pruning), plus its subject mappings.
        await SeedReferencedResource(PartyA, serviceResource, seed: 1);

        using (var scope = Application.GetServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
            db.SubjectResources.AddRange(
                new()
                {
                    Id = Guid.NewGuid(),
                    Resource = serviceResource,
                    Subject = roleSubject,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Resource = serviceResource,
                    Subject = accessPackageSubject,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ConfigureAuthorizedResources(new()
        {
            [PartyA] = [serviceResource]
        });

        var result = await Application.Send(
            new SearchAuthorizedServiceResourcesQuery(),
            TestContext.Current.CancellationToken);

        var item = result.Items.Should().ContainSingle(x => x.ServiceResource.Id == "authsr-enriched").Subject;
        item.ServiceResource.Status.Should().Be("active");
        item.Roles.Should().ContainSingle(x => x.Urn == roleSubject);
        item.AccessPackages.Should().ContainSingle(x => x.Urn == accessPackageSubject);
    }

    /// <summary>
    /// Seeds a dialog so that <paramref name="resource"/> becomes referenced by Dialogporten for
    /// <paramref name="party"/> (via the partyresource trigger). Distinct seeds avoid faker collisions when
    /// seeding multiple dialogs in one test.
    /// </summary>
    private async Task SeedReferencedResource(string party, string resource, int seed) =>
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = party;
                x.Dto.ServiceResource = resource;
            }, seed: seed)
            .ExecuteAsync();

    private static void ConfigureAuthorizedResources(Dictionary<string, HashSet<string>> resourcesByParties) =>
        DialogApplication.AltinnAuthorization.Configure(x =>
            x.ConfigureGetAuthorizedResourcesForSearch(new DialogSearchAuthorizationResult
            {
                ResourcesByParties = resourcesByParties
            }));
}
