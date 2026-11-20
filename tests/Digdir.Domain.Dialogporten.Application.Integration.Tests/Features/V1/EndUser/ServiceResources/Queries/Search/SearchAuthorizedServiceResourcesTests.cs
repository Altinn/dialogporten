using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.ServiceResources.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchAuthorizedServiceResourcesTests(DialogApplication application)
    : ApplicationCollectionFixture(application)
{
    private const string PartyA = "urn:altinn:organization:identifier-no:111111111";
    private const string PartyB = "urn:altinn:organization:identifier-no:222222222";
    private const string ResourceA = "urn:altinn:resource:authsr-a";
    private const string ResourceB = "urn:altinn:resource:authsr-b";

    [Fact]
    public async Task Returns_Empty_When_No_Authorizations()
    {
        await FlowBuilder.For(Application)
            .EnableCaching()
            .Do(_ => ConfigureAuthorizedResources(new()))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery())
            .ExecuteAndAssert(result => result.Items.Should().BeEmpty());
    }

    [Fact]
    public async Task Returns_Union_Of_Authorized_Resources_Across_Parties()
    {
        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog(ReferencedResource(PartyA, ResourceA), seed: 1)
            .CreateSimpleDialog(ReferencedResource(PartyB, ResourceB), seed: 2)
            .Do(_ => ConfigureAuthorizedResources(new()
            {
                [PartyA] = [ResourceA],
                [PartyB] = [ResourceB]
            }))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery())
            .ExecuteAndAssert(result => result.Items.Select(x => x.ServiceResource.Id)
                .Should().BeEquivalentTo("authsr-a", "authsr-b"));
    }

    [Fact]
    public async Task Party_Filter_Narrows_Result()
    {
        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog(ReferencedResource(PartyA, ResourceA), seed: 1)
            .CreateSimpleDialog(ReferencedResource(PartyB, ResourceB), seed: 2)
            .Do(_ => ConfigureAuthorizedResources(new()
            {
                [PartyA] = [ResourceA],
                [PartyB] = [ResourceB]
            }))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery { Parties = [PartyA] })
            .ExecuteAndAssert(result => result.Items.Should().ContainSingle()
                .Which.ServiceResource.Id.Should().Be("authsr-a"));
    }

    [Fact]
    public async Task Unknown_Party_Filter_Is_Silently_Dropped()
    {
        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog(ReferencedResource(PartyA, ResourceA), seed: 1)
            .Do(_ => ConfigureAuthorizedResources(new()
            {
                [PartyA] = [ResourceA]
            }))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery
            {
                Parties = ["urn:altinn:organization:identifier-no:999999999"]
            })
            .ExecuteAndAssert(result => result.Items.Should().BeEmpty());
    }

    [Fact]
    public async Task Does_Not_Return_Resources_Not_Referenced_By_Dialogporten()
    {
        // No dialog seeded for ResourceA, so it is not in the referenced catalogue. Even though the caller is
        // authorized for a single resource (well below the pruning threshold), it must be pruned away.
        await FlowBuilder.For(Application)
            .EnableCaching()
            .Do(_ => ConfigureAuthorizedResources(new()
            {
                [PartyA] = [ResourceA]
            }))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery())
            .ExecuteAndAssert(result => result.Items.Should().BeEmpty());
    }

    [Fact]
    public async Task Enriches_Authorized_Resource_With_Roles_And_AccessPackages()
    {
        const string serviceResource = "urn:altinn:resource:authsr-enriched";
        const string roleSubject = "urn:altinn:rolecode:DIALOG_READ";
        const string accessPackageSubject = "urn:altinn:accesspackage:dialog_lookup_package";

        // Seed a dialog for PartyA so the resource is referenced in the partyresource schema (and thus survives
        // pruning), plus its subject mappings.
        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog(ReferencedResource(PartyA, serviceResource), seed: 1)
            .Do(async _ =>
            {
                using var scope = Application.GetServiceProvider().CreateScope();
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
            })
            .Do(_ => ConfigureAuthorizedResources(new()
            {
                [PartyA] = [serviceResource]
            }))
            .UpdateCatalogueCache()
            .SendCommand(_ => new SearchAuthorizedServiceResourcesQuery())
            .ExecuteAndAssert(result =>
            {
                var item = result.Items.Should().ContainSingle(x => x.ServiceResource.Id == "authsr-enriched").Subject;
                item.ServiceResource.Status.Should().Be("active");
                item.Roles.Should().ContainSingle(x => x.Urn == roleSubject);
                item.AccessPackages.Should().ContainSingle(x => x.Urn == accessPackageSubject);
            });
    }

    /// <summary>
    /// Makes the created dialog reference <paramref name="resource"/> for <paramref name="party"/> (via the
    /// partyresource trigger). Use distinct seeds when creating multiple dialogs in one flow to avoid faker
    /// collisions.
    /// </summary>
    private static Action<CreateDialogCommand, FlowContext> ReferencedResource(string party, string resource) =>
        (x, _) =>
        {
            x.Dto.Party = party;
            x.Dto.ServiceResource = resource;
        };

    private static void ConfigureAuthorizedResources(Dictionary<string, HashSet<string>> resourcesByParties) =>
        DialogApplication.AltinnAuthorization.Configure(x => x.ConfigureGetAuthorizedResourcesForSearch(
            new DialogSearchAuthorizationResult
            {
                ResourcesByParties = resourcesByParties
                    .ToDictionary(kv => kv.Key, kv => (IReadOnlySet<string>)kv.Value)
            }));
}
