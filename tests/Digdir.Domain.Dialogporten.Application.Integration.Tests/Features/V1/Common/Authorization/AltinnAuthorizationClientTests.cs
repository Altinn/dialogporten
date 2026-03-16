using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZiggyCreatures.Caching.Fusion;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Authorization;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class AltinnAuthorizationClientTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task UserHasRequiredAuthLevel_Should_Return_False_When_Resource_Policy_Information_Is_Missing()
    {
        // Arrange
        var client = CreateAltinnAuthorizationClient(Constants.IdportenLoaHigh);

        // Act
        var result = await client.UserHasRequiredAuthLevel(
            "urn:altinn:resource:does-not-exist",
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(3, Constants.IdportenLoaHigh, true)] // user level 4 >= required 3
    [InlineData(4, Constants.IdportenLoaHigh, true)] // user level 4 >= required 4
    [InlineData(4, Constants.IdportenLoaSubstantial, false)] // user level 3 < required 4
    [InlineData(3, Constants.IdportenLoaSubstantial, true)]  // user level 3 >= required 3
    public async Task UserHasRequiredAuthLevel_Should_Respect_MinimumAuthenticationLevel(
        int minimumAuthenticationLevel, string userAuthLevel, bool expected)
    {
        // Arrange
        var serviceResource = $"urn:altinn:resource:test-{Guid.NewGuid()}";
        using var scope = Application.GetServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
        db.ResourcePolicyInformation.Add(new Domain.ResourcePolicyInformation.ResourcePolicyInformation
        {
            Resource = serviceResource,
            MinimumAuthenticationLevel = minimumAuthenticationLevel
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var client = CreateAltinnAuthorizationClient(userAuthLevel, db);

        // Act
        var result = await client.UserHasRequiredAuthLevel(
            serviceResource,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expected);
    }

    private static AltinnAuthorizationClient CreateAltinnAuthorizationClient(
        string userAuthLevel, DialogDbContext db)
    {
        var cacheProvider = Substitute.For<IFusionCacheProvider>();
        cacheProvider.GetCache(Arg.Any<string>()).Returns(Substitute.For<IFusionCache>());

        var user = Substitute.For<IUser>();
        user.GetPrincipal().Returns(TestUsers.FromDefault()
            .WithClaim(ClaimsPrincipalExtensions.IdportenAuthLevelClaim, userAuthLevel)
            .Build());

        return new AltinnAuthorizationClient(
            new HttpClient(),
            cacheProvider,
            user,
            db,
            Substitute.For<IResourceRegistry>(),
            Substitute.For<IPartyResourceReferenceRepository>(),
            Substitute.For<ILogger<AltinnAuthorizationClient>>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IOptionsMonitor<ApplicationSettings>>());
    }
}
