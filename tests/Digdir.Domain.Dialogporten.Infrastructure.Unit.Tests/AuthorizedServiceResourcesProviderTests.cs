using System.Diagnostics;
using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizedServiceResourcesProviderTests
{
    private const string Pid = "22834498646";
    private const string OtherPid = "13213312833";
    private const string PartyA = "urn:altinn:organization:identifier-no:111111111";
    private const string ReferencedResource = "urn:altinn:resource:referenced";

    [Fact]
    public async Task Requests_Threshold_Zero_Pruning_From_The_Search_Call()
    {
        // Pruning is done once inside GetAuthorizedResourcesForSearch; the provider must request it
        // unconditionally (threshold 0) so the result is restricted to the referenced catalogue regardless of
        // size, and must not prune a second time. The stub returns the already-pruned set.
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>> { [PartyA] = new HashSet<string> { ReferencedResource } }
        });
        var provider = new AuthorizedServiceResourcesProvider(
            authorization, CreateUserWithPid(Pid), CreateUserParties(partyCount: 1), CreateSettings(), CreateCacheProvider());

        var result = await provider.GetAuthorizedServiceResources(partyFilter: null, TestContext.Current.CancellationToken);

        authorization.LastMinResourcesPruningThreshold.Should().Be(0);
        result.IncludeFullCatalogue.Should().BeFalse();
        result.ResourceUrns.Should().BeEquivalentTo([ReferencedResource]);
        // DialogIds are not needed for this endpoint, so they are not resolved.
        authorization.LastIncludeDialogIds.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_Full_Catalogue_Signal_When_Party_Count_Exceeds_Limit_On_Unfiltered_Request()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult());
        var provider = new AuthorizedServiceResourcesProvider(
            authorization, CreateUserWithPid(Pid), CreateUserParties(partyCount: 3),
            CreateSettings(maxAuthorizedParties: 2), CreateCacheProvider());

        var result = await provider.GetAuthorizedServiceResources(partyFilter: null, TestContext.Current.CancellationToken);

        // 3 parties > limit of 2 on an unfiltered request -> return the full catalogue. The party count comes from
        // the lightweight authorized-parties lookup, so the expensive resolution + pruning is skipped entirely.
        result.IncludeFullCatalogue.Should().BeTrue();
        result.ResourceUrns.Should().BeEmpty();
        authorization.CallCount.Should().Be(0); // GetAuthorizedResourcesForSearch was never called
    }

    [Fact]
    public async Task Pushes_Party_Filter_Down_As_Constraint_Parties()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>> { [PartyA] = new HashSet<string> { ReferencedResource } }
        });
        var provider = new AuthorizedServiceResourcesProvider(
            authorization, CreateUserWithPid(Pid), CreateUserParties(partyCount: 1), CreateSettings(), CreateCacheProvider());

        var result = await provider.GetAuthorizedServiceResources([PartyA], TestContext.Current.CancellationToken);

        // A filtered request resolves only the requested party (constraint pushed down), not every authorized
        // party — so a filtered request by a whale user stays cheap and bounded.
        authorization.LastConstraintParties.Should().BeEquivalentTo([PartyA]);
        result.IncludeFullCatalogue.Should().BeFalse();
        result.ResourceUrns.Should().BeEquivalentTo([ReferencedResource]);
    }

    [Fact]
    public async Task Caches_Result_Per_Caller()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>> { [PartyA] = new HashSet<string> { ReferencedResource } }
        });
        var provider = new AuthorizedServiceResourcesProvider(
            authorization, CreateUserWithPid(Pid), CreateUserParties(partyCount: 1), CreateSettings(), CreateCacheProvider());

        await provider.GetAuthorizedServiceResources(partyFilter: null, TestContext.Current.CancellationToken);
        var second = await provider.GetAuthorizedServiceResources(partyFilter: null, TestContext.Current.CancellationToken);

        // Second call for the same caller is served from cache; the upstream authorization runs only once.
        second.ResourceUrns.Should().BeEquivalentTo([ReferencedResource]);
        authorization.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Cache_Is_Keyed_By_Caller_And_Filter()
    {
        // One shared cache provider across separate provider instances, so a hit/miss reflects the cache KEY
        // (caller identity + normalized filter), not instance-local state.
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>> { [PartyA] = new HashSet<string> { ReferencedResource } }
        });
        var cacheProvider = CreateCacheProvider();

        AuthorizedServiceResourcesProvider ProviderFor(string pid)
        {
            return new AuthorizedServiceResourcesProvider(
                authorization, CreateUserWithPid(pid), CreateUserParties(partyCount: 1), CreateSettings(), cacheProvider);
        }

        var cancellationToken = TestContext.Current.CancellationToken;

        // Same caller, same (unfiltered) key -> served from cache, factory runs once.
        await ProviderFor(Pid).GetAuthorizedServiceResources(partyFilter: null, cancellationToken);
        await ProviderFor(Pid).GetAuthorizedServiceResources(partyFilter: null, cancellationToken);
        authorization.CallCount.Should().Be(1);

        // A different caller has a different cache key -> the factory runs again (no cross-caller leakage).
        await ProviderFor(OtherPid).GetAuthorizedServiceResources(partyFilter: null, cancellationToken);
        authorization.CallCount.Should().Be(2);

        // The same caller with a party filter is keyed separately from its unfiltered entry -> runs again.
        var filtered = await ProviderFor(Pid).GetAuthorizedServiceResources([PartyA], cancellationToken);
        authorization.CallCount.Should().Be(3);
        filtered.ResourceUrns.Should().BeEquivalentTo([ReferencedResource]);
    }

    [Fact]
    public async Task Throws_For_Principal_Without_End_User_Party_Identifier()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult());
        var provider = new AuthorizedServiceResourcesProvider(
            authorization,
            new StubUser(new ClaimsPrincipal(new ClaimsIdentity())),
            CreateUserParties(partyCount: 0),
            CreateSettings(),
            CreateCacheProvider());

        var act = () => provider.GetAuthorizedServiceResources(partyFilter: null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnreachableException>();
        // The throw happens before any cache access or upstream call.
        authorization.CallCount.Should().Be(0);
    }

    private static IFusionCacheProvider CreateCacheProvider() =>
        TestFusionCache.CreateProvider(AuthorizedServiceResourcesProvider.CacheName);

    private static StubOptionsSnapshot<ApplicationSettings> CreateSettings(int maxAuthorizedParties = 1000)
    {
        var settings = new ApplicationSettings
        {
            Dialogporten = null!, // not read by the provider
            Limits = new LimitsSettings
            {
                AuthorizedServiceResources = new AuthorizedServiceResourceLimits
                {
                    MaxAuthorizedPartiesBeforeFullCatalogue = maxAuthorizedParties
                }
            }
        };
        return new StubOptionsSnapshot<ApplicationSettings>(settings);
    }

    private static StubUser CreateUserWithPid(string pid) =>
        new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsPrincipalExtensions.PidClaim, pid)])));

    private static StubUserParties CreateUserParties(int partyCount) => new(partyCount);

    private sealed class StubUserParties(int partyCount) : IUserParties
    {
        public Task<AuthorizedPartiesResult> GetUserParties(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthorizedPartiesResult
            {
                AuthorizedParties = Enumerable.Range(0, partyCount).Select(i => new AuthorizedParty
                {
                    Party = $"urn:altinn:organization:identifier-no:{100_000_000 + i}",
                    PartyUuid = Guid.NewGuid(),
                    PartyId = i,
                    Name = "Party",
                    DateOfBirth = null,
                    PartyType = AuthorizedPartyType.Organization,
                    IsDeleted = false,
                    HasKeyRole = false,
                    IsCurrentEndUser = false,
                    IsMainAdministrator = false,
                    IsAccessManager = false,
                    HasOnlyAccessToSubParties = false,
                    AuthorizedResources = [],
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedInstances = []
                }).ToList()
            });
    }

    private sealed class CountingAltinnAuthorization(DialogSearchAuthorizationResult result) : IAltinnAuthorization
    {
        public int CallCount { get; private set; }
        public bool? LastIncludeDialogIds { get; private set; }
        public List<string>? LastConstraintParties { get; private set; }
        public int? LastMinResourcesPruningThreshold { get; private set; }

        public Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(
            List<string> constraintParties,
            List<string> constraintServiceResources,
            bool includeDialogIds = true,
            int? minResourcesPruningThreshold = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastIncludeDialogIds = includeDialogIds;
            LastConstraintParties = constraintParties;
            LastMinResourcesPruningThreshold = minResourcesPruningThreshold;

            // Honor the pushed-down party constraint, like the production AltinnAuthorizationClient and
            // LocalDevelopmentAltinnAuthorization do, so the provider can trust the constraint and the result is
            // scoped to the requested parties.
            if (constraintParties.Count == 0)
            {
                return Task.FromResult(result);
            }

            return Task.FromResult(new DialogSearchAuthorizationResult
            {
                ResourcesByParties = result.ResourcesByParties
                    .Where(kv => constraintParties.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                DialogIds = result.DialogIds
            });
        }

        public Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
            DialogEntity dialogEntity, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AuthorizedPartiesResult> GetAuthorizedParties(
            IPartyIdentifier authenticatedParty, bool flatten = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
            IPartyIdentifier authenticatedParty, List<string> constraintParties, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasListAuthorizationForDialog(
            DialogEntity dialog, CancellationToken cancellationToken) => throw new NotSupportedException();

        public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) => throw new NotSupportedException();

        public Task<bool> UserHasRequiredAuthLevel(
            string serviceResource, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
