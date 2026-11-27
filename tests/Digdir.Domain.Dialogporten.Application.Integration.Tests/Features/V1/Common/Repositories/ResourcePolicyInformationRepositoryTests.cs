using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Repositories;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ResourcePolicyInformationRepositoryTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string FirstResource = "urn:altinn:resource:test-policy-merge-first";
    private const string SecondResource = "urn:altinn:resource:test-policy-merge-second";

    [Fact]
    public async Task Merge_Should_Insert_And_Update_SystemUser_Level()
    {
        // Arrange
        using var scope = Application.GetServiceProvider().CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IResourcePolicyInformationRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act: insert, then update with the system-user levels swapped, as when a service owner
        // turns the flag on for one resource and off for another
        await sut.Merge([Create(FirstResource, 4, 3), Create(SecondResource, 4, null)], cancellationToken);
        var inserted = await sut.GetMinimumAuthenticationLevels(cancellationToken);

        await sut.Merge([Create(FirstResource, 4, null), Create(SecondResource, 4, 3)], cancellationToken);
        var updated = await sut.GetMinimumAuthenticationLevels(cancellationToken);

        // Assert
        inserted[FirstResource].Should().Be(new MinimumAuthenticationLevels(4, 3));
        inserted[SecondResource].Should().Be(new MinimumAuthenticationLevels(4, null));
        updated[FirstResource].Should().Be(new MinimumAuthenticationLevels(4, null));
        updated[SecondResource].Should().Be(new MinimumAuthenticationLevels(4, 3));
    }

    private static ResourcePolicyInformation Create(
        string resource,
        int minimumAuthenticationLevel,
        int? minimumSystemUserAuthenticationLevel) => new()
        {
            Id = Guid.CreateVersion7(),
            Resource = resource,
            MinimumAuthenticationLevel = minimumAuthenticationLevel,
            MinimumSystemUserAuthenticationLevel = minimumSystemUserAuthenticationLevel,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
