using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Metadata.ServiceResources.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetServiceResourceMetadataTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Should_Read_ServiceResources_From_PartyResource_Schema()
    {
        const string serviceResource = "urn:altinn:resource:test-service-metadata";
        const string unprefixedServiceResource = "test-service-metadata";
        const string roleSubject = "urn:altinn:rolecode:DIALOG_READ";
        const string accessPackageSubject = "urn:altinn:accesspackage:dialog_lookup_package";

        await SetupDatabase(serviceResource, roleSubject, accessPackageSubject);

        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = serviceResource)
            .ExecuteAsync();

        using var queryScope = Application.GetServiceProvider().CreateScope();
        var sender = queryScope.ServiceProvider.GetRequiredService<ISender>();
        var catalogue = queryScope.ServiceProvider.GetRequiredService<IServiceResourceMetadataCatalogue>();

        await catalogue.UpdateCatalogueCache(TestContext.Current.CancellationToken);
        var result = await sender.Send(new GetServiceResourceMetadataQuery(), TestContext.Current.CancellationToken);

        var item = result.Items.Should().ContainSingle(x => x.ServiceResource.Id == unprefixedServiceResource).Subject;
        item.ServiceResource.MinimumAuthenticationLevel.Should().Be(4);
        item.ServiceResource.Links.Should().NotBeNull();
        item.ServiceResource.Links.Metadata.Should().Be("https://platform.example/resourceregistry/api/v1/resource/test-service-metadata");
        item.Roles.Should().ContainSingle(x => x.Urn == roleSubject);
        item.AccessPackages.Should().ContainSingle(x => x.Urn == accessPackageSubject);
    }

    [Fact]
    public async Task Cancelled_Request_Should_Not_Affect_Catalogue_Cache_Write()
    {
        const string serviceResource = "urn:altinn:resource:test-service-metadata";
        const string unprefixedServiceResource = "test-service-metadata";
        const string roleSubject = "urn:altinn:rolecode:DIALOG_READ";
        const string accessPackageSubject = "urn:altinn:accesspackage:dialog_lookup_package";

        await SetupDatabase(serviceResource, roleSubject, accessPackageSubject);

        await FlowBuilder.For(Application)
            .EnableCaching()
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = serviceResource)
            .Do(async (_, _) =>
            {
                using var queryScope = Application.GetServiceProvider().CreateScope();
                var sender = queryScope.ServiceProvider.GetRequiredService<ISender>();
                var catalogue = queryScope.ServiceProvider.GetRequiredService<IServiceResourceMetadataCatalogue>();

                // The cache write runs on its own token, like ServiceResourceMetadataCatalogueHostedService does
                // with its stopping token, while the request that is cancelled has an unrelated token.
                var cacheWrite = catalogue.UpdateCatalogueCache(TestContext.Current.CancellationToken);

                using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                await requestCts.CancelAsync();
                var cancelledRequest = () => sender.Send(new GetServiceResourceMetadataQuery(), requestCts.Token);

                await cancelledRequest.Should().ThrowAsync<OperationCanceledException>();
                await cacheWrite;

                var result = await sender.Send(new GetServiceResourceMetadataQuery(), TestContext.Current.CancellationToken);
                var item = result.Items.Should().ContainSingle(x => x.ServiceResource.Id == unprefixedServiceResource).Subject;
                item.ServiceResource.MinimumAuthenticationLevel.Should().Be(4);
                item.ServiceResource.Links.Should().NotBeNull();
                item.ServiceResource.Links.Metadata.Should().Be("https://platform.example/resourceregistry/api/v1/resource/test-service-metadata");
                item.Roles.Should().ContainSingle(x => x.Urn == roleSubject);
                item.AccessPackages.Should().ContainSingle(x => x.Urn == accessPackageSubject);
            })
            .ExecuteAsync();
    }

    private async Task SetupDatabase(string serviceResource, string roleSubject, string accessPackageSubject)
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
        db.ResourcePolicyInformation.Add(new()
        {
            Id = Guid.NewGuid(),
            Resource = serviceResource,
            MinimumAuthenticationLevel = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
