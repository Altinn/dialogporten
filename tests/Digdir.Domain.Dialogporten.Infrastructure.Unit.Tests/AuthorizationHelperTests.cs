using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizationHelperTests
{
    [Fact]
    public async Task PruneUnreferencedResources_ShouldReturnEarly_WhenNoParties()
    {
        var result = new DialogSearchAuthorizationResult();
        var repo = new FakePartyResourceReferenceRepository();

        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            repo,
            minResourcesPruningThreshold: 0,
            CancellationToken.None);

        Assert.Equal(0, repo.GetReferencedResourcesByPartyCallCount);
    }

    [Fact]
    public async Task PruneUnreferencedResources_ShouldReturnEarly_WhenDistinctResourcesAtOrBelowThreshold()
    {
        var result = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1", "resource2"],
                ["party2"] = ["resource2"]
            }
        };
        var repo = new FakePartyResourceReferenceRepository();

        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            repo,
            minResourcesPruningThreshold: 2,
            CancellationToken.None);

        Assert.Equal(0, repo.GetReferencedResourcesByPartyCallCount);
        Assert.Equal(2, result.ResourcesByParties.Count);
    }

    [Fact]
    public async Task PruneUnreferencedResources_ShouldPruneByIntersection_AndKeepInstanceIds()
    {
        var result = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1", "resource2"],
                ["party2"] = ["resource3"],
                ["party3"] = ["resource4"],
            },
            AltinnAppInstanceIds =
            [
                $"{Constants.ServiceContextInstanceIdPrefix}111/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
            ]
        };

        var repo = new FakePartyResourceReferenceRepository
        {
            ReferencedResourcesByParty = new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource2"],
                ["party3"] = ["resource4"],
            }
        };

        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            repo,
            minResourcesPruningThreshold: 0,
            CancellationToken.None);

        Assert.Equal(1, repo.GetReferencedResourcesByPartyCallCount);
        Assert.Equal(2, result.ResourcesByParties.Count);
        Assert.Single(result.ResourcesByParties["party1"]);
        Assert.Contains("resource2", result.ResourcesByParties["party1"]);
        Assert.Single(result.ResourcesByParties["party3"]);
        Assert.Contains("resource4", result.ResourcesByParties["party3"]);
        Assert.Single(result.AltinnAppInstanceIds);
        Assert.Equal(
            $"{Constants.ServiceContextInstanceIdPrefix}111/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            result.AltinnAppInstanceIds.Single());
    }

    [Fact]
    public async Task PruneUnreferencedResources_ShouldPassDistinctResourceSetToRepository()
    {
        var result = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>
            {
                ["party1"] = ["resource1"],
                ["party2"] = ["resource1", "resource2"]
            }
        };
        var repo = new FakePartyResourceReferenceRepository();

        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            repo,
            minResourcesPruningThreshold: 0,
            CancellationToken.None);

        Assert.Equal(1, repo.GetReferencedResourcesByPartyCallCount);
        Assert.NotNull(repo.LastRequestedParties);
        Assert.NotNull(repo.LastRequestedResources);
        Assert.Equal(2, repo.LastRequestedParties.Count);
        Assert.Equal(2, repo.LastRequestedResources.Count);
        Assert.Contains("resource1", repo.LastRequestedResources);
        Assert.Contains("resource2", repo.LastRequestedResources);
    }

    [Fact]
    public async Task ResolveDialogSearchAuthorization_ThenPruneUnreferencedResources_ShouldPruneExpectedServiceResources()
    {
        const string party = "urn:altinn:organization:identifier-no:313130983";
        const string resourceA = "urn:altinn:resource:resource-a";
        const string resourceB = "urn:altinn:resource:resource-b";

        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties =
            [
                new()
                {
                    Party = party,
                    PartyId = 313130983,
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedResources = [resourceA, resourceB],
                    AuthorizedInstances = []
                }
            ]
        };

        var resolved = await AuthorizationHelper.ResolveDialogSearchAuthorization(
            authorizedParties,
            constraintParties: [],
            constraintResources: [],
            _ => Task.FromResult(new List<SubjectResource>()),
            CancellationToken.None);

        var repo = new FakePartyResourceReferenceRepository
        {
            ReferencedResourcesByParty = new Dictionary<string, HashSet<string>>
            {
                [party] = [resourceB]
            }
        };

        await AuthorizationHelper.PruneUnreferencedResources(
            resolved,
            repo,
            minResourcesPruningThreshold: 0,
            CancellationToken.None);

        Assert.Equal(1, repo.GetReferencedResourcesByPartyCallCount);
        Assert.True(resolved.ResourcesByParties.TryGetValue(party, out var prunedResources));
        Assert.Single(prunedResources);
        Assert.Contains(resourceB, prunedResources);
    }

    [Theory]
    [MemberData(nameof(ResolvingScenarios))]
    public async Task ResolveDialogSearchAuthorization_ShouldResolveCorrectly(
        string _, // Used for test explorer readability
        List<string> constraintParties,
        List<string> constraintResources,
        Dictionary<string, List<string>> expectedResourcesByParties,
        List<string> expectedInstanceIds)
    {
        // Arrange
        var authorizedParties = GetAuthorizedParties();

        // Act
        var actualResult = await AuthorizationHelper.ResolveDialogSearchAuthorization(
            authorizedParties,
            constraintParties,
            constraintResources,
            GetSubjectResources,
            CancellationToken.None);

        // Assert
        // 1. Ensure the number of parties in the result is as expected.
        Assert.Equal(expectedResourcesByParties.Count, actualResult.ResourcesByParties.Count);

        // 2. Iterate through each expected party and its resources for detailed validation.
        foreach (var (expectedParty, expectedResources) in expectedResourcesByParties)
        {
            // Verify the party is in the actual result
            Assert.Contains(expectedParty, actualResult.ResourcesByParties.Keys);
            var actualResources = actualResult.ResourcesByParties[expectedParty];

            // Verify the resource count for the party is correct
            Assert.Equal(expectedResources.Count, actualResources.Count);

            // Verify that all expected resources are present (order doesn't matter)
            Assert.Empty(expectedResources.Except(actualResources));
        }

        // 3. Assert Altinn App Instance IDs
        Assert.Equal(expectedInstanceIds.Count, actualResult.AltinnAppInstanceIds.Count);
        Assert.Empty(expectedInstanceIds.Except(actualResult.AltinnAppInstanceIds));
    }

    public static IEnumerable<object[]> ResolvingScenarios =>
        new List<object[]>
        {
            new object[]
            {
                "No constraints applied",
                new List<string>(),
                new List<string>(),
                new Dictionary<string, List<string>>
                {
                    ["party1"] = ["resource1", "resource2", "resource3", "resource4", "resource5", "resource6"],
                    ["party2"] = [ "resource1", "resource2", "resource3", "resource4", "resource5", "resource6", "resource7", "resource8" ],
                    ["party3"] = ["resource5", "resource6"],
                    ["party4"] = ["resource7"]
                },
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000001",
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000002",
                    Constants.ServiceContextInstanceIdPrefix + "222/00000000-0000-0000-0000-000000000003",
                    Constants.ServiceContextInstanceIdPrefix + "444/00000000-0000-0000-0000-000000000004",
                }
            },

            new object[]
            {
                "With party constraints, without resource constraints",
                new List<string> { "party1", "party2" },
                new List<string>(),
                new Dictionary<string, List<string>>
                {
                    ["party1"] = ["resource1", "resource2", "resource3", "resource4", "resource5", "resource6"],
                    ["party2"] = ["resource1", "resource2", "resource3", "resource4", "resource5", "resource6", "resource7", "resource8" ],
                },
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000001",
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000002",
                    Constants.ServiceContextInstanceIdPrefix + "222/00000000-0000-0000-0000-000000000003"
                }
            },

            new object[]
            {
                "Without party constraints, with resource constraints",
                new List<string>(),
                new List<string> { "resource1", "resource2", "resource5" },
                new Dictionary<string, List<string>>
                {
                    ["party1"] = ["resource1", "resource2", "resource5"],
                    ["party2"] = ["resource1", "resource2", "resource5"],
                    ["party3"] = ["resource5"]
                },
                new List<string>()
            },

            new object[]
            {
                "With both party and resource constraints",
                new List<string> { "party2" },
                new List<string> { "resource4" },
                new Dictionary<string, List<string>>
                {
                    ["party2"] = ["resource4"]
                },
                new List<string>()
            },

            new object[]
            {
                "With instance resource constraint",
                new List<string>(),
                new List<string> { "urn:altinn:resource:app_org_app-2" },
                new Dictionary<string, List<string>>(),
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000002",
                    Constants.ServiceContextInstanceIdPrefix + "222/00000000-0000-0000-0000-000000000003"
                }
            },

            new object[]
            {
                "With party and instance resource constraint",
                new List<string> { "party1" },
                new List<string> { "urn:altinn:resource:app_org_app-2" },
                new Dictionary<string, List<string>>(),
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/00000000-0000-0000-0000-000000000002",
                }
            },

            new object[]
            {
                "With party constraint and without matching instance resource constraint",
                new List<string> { "party2" },
                new List<string> { "urn:altinn:resource:app_org_app-1" },
                new Dictionary<string, List<string>>(),
                new List<string>()
            }
        };


    private static Task<List<SubjectResource>> GetSubjectResources(CancellationToken token)
    {
        /* The mocked mapping of subjects to resources is as follows:
         *
         * role1: resource1, resource2
         * role2: resource2, resource3, resource4
         * role3: resource5
         * accesspackage1: resource1, resource6
         * accesspackage2: resource7, resource8
         */
        return Task.FromResult(new List<SubjectResource>
        {
            new() { Subject = "role1", Resource = "resource1" },
            new() { Subject = "role1", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource3" },
            new() { Subject = "role2", Resource = "resource4" },
            new() { Subject = "role3", Resource = "resource5" },
            new() { Subject = "accesspackage1", Resource = "resource1" },
            new() { Subject = "accesspackage1", Resource = "resource6" },
            new() { Subject = "accesspackage2", Resource = "resource7" },
            new() { Subject = "accesspackage2", Resource = "resource8" }
        });
    }

    private static AuthorizedPartiesResult GetAuthorizedParties()
    {
        return new AuthorizedPartiesResult
        {
            AuthorizedParties =
            [
                new()
                {
                    /*
                     * Should be flattened to:
                     * - resource1 (from role1, accesspackage1, AuthorizedResources)
                     * - resource2 (from role1, role2)
                     * - resource3 (from role2)
                     * - resource4 (from role2)
                     * - resource5 (from AuthorizedResources)
                     * - resource6 (from accesspackage1)
                     */
                    Party = "party1",
                    PartyId = 111,
                    AuthorizedRolesAndAccessPackages = ["role1", "role2", "accesspackage1"],
                    AuthorizedResources = ["resource1", "resource5"],
                    AuthorizedInstances =
                    [
                        new() { InstanceId = "00000000-0000-0000-0000-000000000001", ResourceId = "app_org_app-1" },
                        new() { InstanceId = "00000000-0000-0000-0000-000000000002", ResourceId = "app_org_app-2" }
                    ]
                },

                new()
                {
                    /*
                     * Should be flattened to:
                     * - resource1 (from accesspackage1, AuthorizedResources)
                     * - resource2 (from role2, AuthorizedResources)
                     * - resource3 (from role2)
                     * - resource4 (from role2)
                     * - resource5 (from AuthorizedResources)
                     * - resource6 (from accesspackage1)
                     * - resource7 (from accesspackage2)
                     * - resource8 (from accesspackage2)
                     */
                    Party = "party2",
                    PartyId = 222,
                    AuthorizedRolesAndAccessPackages = ["role2", "accesspackage1", "accesspackage2"],
                    AuthorizedResources = ["resource1", "resource2", "resource5"],
                    AuthorizedInstances =
                    [
                        new() { InstanceId = "00000000-0000-0000-0000-000000000003", ResourceId = "app_org_app-2" }
                    ]
                },

                new()
                {
                    /*
                     * Should be flattened to:
                     * - resource5 (from role3)
                     * - resource6 (from AuthorizedResources)
                     */
                    Party = "party3",
                    PartyId = 333,
                    AuthorizedRolesAndAccessPackages = ["role3"],
                    AuthorizedResources = ["resource6"]
                },

                new()
                {
                    /*
                     * Should be flattened to:
                     * - resource7 (from AuthorizedResources)
                     */
                    Party = "party4",
                    PartyId = 444,
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedResources = ["resource7"],
                    AuthorizedInstances =
                    [
                        new() { InstanceId = "00000000-0000-0000-0000-000000000004", ResourceId = "app_org_app-3" },
                        new() { InstanceId = "00000000-0000-0000-0000-000000000005", ResourceId = "not-an-app" },
                        new() { InstanceId = "invalid-instance-id", ResourceId = "app_org_app-3" }
                    ]
                }
            ]
        };
    }

    private sealed class FakePartyResourceReferenceRepository : IPartyResourceReferenceRepository
    {
        internal int GetReferencedResourcesByPartyCallCount { get; private set; }
        internal List<string>? LastRequestedParties { get; private set; }
        internal List<string>? LastRequestedResources { get; private set; }

        internal Dictionary<string, HashSet<string>> ReferencedResourcesByParty { get; init; } = [];

        public Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
            IReadOnlyCollection<string> parties,
            IReadOnlyCollection<string> resources,
            CancellationToken cancellationToken)
        {
            GetReferencedResourcesByPartyCallCount++;
            LastRequestedParties = [.. parties];
            LastRequestedResources = [.. resources];
            return Task.FromResult(ReferencedResourcesByParty);
        }

        public Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
