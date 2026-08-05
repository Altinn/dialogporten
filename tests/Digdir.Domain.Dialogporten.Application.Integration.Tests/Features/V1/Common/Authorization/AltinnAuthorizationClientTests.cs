using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;
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

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task GetAuthorizedPartiesForLookup_Should_Respect_SystemUser_PartyFilter_FeatureToggle(
        bool enablePartyFiltersForSystemUsers,
        int expectedPartyFilterCount)
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var client = CreateAltinnAuthorizationClient(
            handler,
            new FeatureToggle
            {
                EnablePartyFiltersForSystemUsers = enablePartyFiltersForSystemUsers
            });

        const string party = "urn:altinn:organization:identifier-no:991825827";

        // Act
        await client.GetAuthorizedPartiesForLookup(
            CreateSystemUserIdentifier(),
            [party],
            TestContext.Current.CancellationToken);

        // Assert
        handler.RequestContent.Should().NotBeNull();
        var requestJson = JsonNode.Parse(handler.RequestContent!)!;
        var partyFilter = requestJson["partyFilter"]!.AsArray();
        partyFilter.Count.Should().Be(expectedPartyFilterCount);

        if (enablePartyFiltersForSystemUsers)
        {
            partyFilter[0]!["Type"]!.GetValue<string>().Should().Be(NorwegianOrganizationIdentifier.Prefix);
            partyFilter[0]!["Value"]!.GetValue<string>().Should().Be("991825827");
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task GetAuthorizedPartiesForLookup_Should_Respect_EmailUser_PartyFilter_FeatureToggle(
        bool enablePartyFiltersForEmailUsers,
        int expectedPartyFilterCount)
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler(EmailUserAuthorizedPartiesResponse);
        var client = CreateAltinnAuthorizationClient(
            handler,
            new FeatureToggle
            {
                EnablePartyFiltersForEmailUsers = enablePartyFiltersForEmailUsers
            });

        const string party = "urn:altinn:organization:identifier-no:991825827";

        // Act
        await client.GetAuthorizedPartiesForLookup(
            CreateEmailUserIdentifier(),
            [party],
            TestContext.Current.CancellationToken);

        // Assert
        handler.RequestContent.Should().NotBeNull();
        var requestJson = JsonNode.Parse(handler.RequestContent!)!;
        var partyFilter = requestJson["partyFilter"]!.AsArray();
        partyFilter.Count.Should().Be(expectedPartyFilterCount);
    }

    [Theory]
    [InlineData(false, 2)] // cache bypassed → one upstream request per call
    [InlineData(true, 1)]  // cached → second call served from cache
    public async Task GetAuthorizedParties_Should_Respect_EmailUser_PartyCache_FeatureToggle(
        bool enablePartyCacheForEmailUsers,
        int expectedUpstreamRequestCount)
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler(EmailUserAuthorizedPartiesResponse);
        var client = CreateAltinnAuthorizationClient(
            handler,
            new FeatureToggle
            {
                EnablePartyCacheForEmailUsers = enablePartyCacheForEmailUsers
            },
            useRealPartiesCache: true);

        var emailUser = CreateEmailUserIdentifier();

        // Act
        await client.GetAuthorizedParties(emailUser, cancellationToken: TestContext.Current.CancellationToken);
        await client.GetAuthorizedParties(emailUser, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.RequestCount.Should().Be(expectedUpstreamRequestCount);
    }

    [Fact]
    public async Task GetAuthorizedParties_Should_Use_PartyCache_For_Other_Users_When_EmailUser_PartyCache_Is_Disabled()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var client = CreateAltinnAuthorizationClient(
            handler,
            new FeatureToggle
            {
                EnablePartyCacheForEmailUsers = false
            },
            useRealPartiesCache: true);

        var systemUser = CreateSystemUserIdentifier();

        // Act
        await client.GetAuthorizedParties(systemUser, cancellationToken: TestContext.Current.CancellationToken);
        await client.GetAuthorizedParties(systemUser, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.RequestCount.Should().Be(1);
    }

    private static AltinnAuthorizationClient CreateAltinnAuthorizationClient(
        string userAuthLevel, DialogDbContext db)
    {
        var cacheProvider = Substitute.For<IFusionCacheProvider>();
        cacheProvider
            .GetCache(Arg.Any<string>())
            .Returns(_ => new NullFusionCache(Options.Create(new FusionCacheOptions())));

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
                new ResourcePolicyInformationRepository(db, cacheProvider)),
            Substitute.For<IPartyResourceReferenceRepository>(),
            Substitute.For<ILogger<AltinnAuthorizationClient>>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IOptionsMonitor<ApplicationSettings>>(),
            Substitute.For<IPartyNameRegistry>()
        );
    }

    private static AltinnAuthorizationClient CreateAltinnAuthorizationClient(
        HttpMessageHandler handler,
        FeatureToggle featureToggle,
        bool useRealPartiesCache = false)
    {
        var cacheProvider = Substitute.For<IFusionCacheProvider>();
        cacheProvider.GetCache(Arg.Any<string>()).Returns(_ => useRealPartiesCache
            ? new FusionCache(new FusionCacheOptions())
            : new NullFusionCache(Options.Create(new FusionCacheOptions())));

        var user = Substitute.For<IUser>();
        user.GetPrincipal().Returns(TestUsers.FromDefault().Build());

        var applicationSettings = Substitute.For<IOptionsMonitor<ApplicationSettings>>();
        applicationSettings.CurrentValue.Returns(TestApplicationSettings.CreateDefault(featureToggle: featureToggle));

        return new AltinnAuthorizationClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://altinn.test")
            },
            cacheProvider,
            user,
            Substitute.For<IDialogDbContext>(),
            Substitute.For<IServiceResourceMinimumAuthenticationLevelResolver>(),
            Substitute.For<IPartyResourceReferenceRepository>(),
            Substitute.For<ILogger<AltinnAuthorizationClient>>(),
            Substitute.For<IServiceScopeFactory>(),
            applicationSettings,
            Substitute.For<IPartyNameRegistry>()
        );
    }

    // A non-empty response is required for email users without party filters; an empty
    // authorized parties list is treated as an upstream error for non-system users
    private const string EmailUserAuthorizedPartiesResponse =
        """
        [{
            "name": "Test User",
            "organizationNumber": "",
            "emailId": "test@example.com",
            "partyId": 1,
            "partyUuid": "0290a7cd-9651-4775-8a47-edbb1bbb8a37",
            "type": "SelfIdentified",
            "isDeleted": false,
            "onlyHierarchyElementWithNoAccess": false,
            "authorizedAccessPackages": [],
            "authorizedResources": [],
            "authorizedRoles": [],
            "authorizedInstances": [],
            "subunits": []
        }]
        """;

    private static IPartyIdentifier CreateEmailUserIdentifier()
    {
        var parsed = IdportenEmailUserIdentifier.TryParse(
            $"{IdportenEmailUserIdentifier.PrefixWithSeparator}test@example.com",
            out var emailUserIdentifier);
        parsed.Should().BeTrue();
        emailUserIdentifier.Should().NotBeNull();

        return emailUserIdentifier;
    }

    private static IPartyIdentifier CreateSystemUserIdentifier()
    {
        var parsed = SystemUserIdentifier.TryParse(
            $"{SystemUserIdentifier.PrefixWithSeparator}{Guid.NewGuid()}",
            out var systemUserIdentifier);
        parsed.Should().BeTrue();
        systemUserIdentifier.Should().NotBeNull();

        return systemUserIdentifier;
    }

    private sealed class CapturingHttpMessageHandler(string responseContent = "[]") : HttpMessageHandler
    {
        public string? RequestContent { get; private set; }
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            request.Content.Should().NotBeNull();
            RequestContent = await request.Content.ReadAsStringAsync(cancellationToken);

            return new(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };
        }
    }
}
