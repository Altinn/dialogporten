using Digdir.Domain.Dialogporten.Application.Common.Authorization;
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
    /// <summary>
    /// When no resource policy information row exists for the requested service resource,
    /// the client falls back to <c>DefaultMinimumAuthenticationLevel</c> (3, i.e. "idporten-loa-substantial")
    /// rather than denying access unconditionally. This prevents false authorization failures that would occur
    /// during transient resource-policy sync delays (e.g. on first deploy or when the sync job has not yet run).
    /// Users at level &lt; 3 (e.g. "idporten-loa-low" / email login, both mapped to level 0) are still rejected.
    /// </summary>
    [Theory]
    [InlineData(Constants.IdportenLoaHigh, true)]        // level 4 >= default 3 → granted
    [InlineData(Constants.IdportenLoaSubstantial, true)] // level 3 >= default 3 → granted
    [InlineData(Constants.IdportenLoaLow, false)]        // level 0 < default 3  → denied
    [InlineData(Constants.IdportenLoaEmail, false)]      // level 0 < default 3  → denied
    public async Task UserHasRequiredAuthLevel_Should_Use_Default_Level_When_Resource_Policy_Information_Is_Missing(
        string userAuthLevel, bool expected)
    {
        // Arrange
        using var scope = Application.GetServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
        var client = CreateAltinnAuthorizationClient(userAuthLevel, db);

        // Act
        var result = await client.UserHasRequiredAuthLevel(
            "urn:altinn:resource:does-not-exist",
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expected);
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
        db.ResourcePolicyInformation.Add(new()
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
            new ServiceResourceMinimumAuthenticationLevelResolver(
                db,
                Substitute.For<ILogger<ServiceResourceMinimumAuthenticationLevelResolver>>()),
            Substitute.For<IResourceRegistry>(),
            Substitute.For<IPartyResourceReferenceRepository>(),
            Substitute.For<ILogger<AltinnAuthorizationClient>>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IOptionsMonitor<ApplicationSettings>>());
    }
}
