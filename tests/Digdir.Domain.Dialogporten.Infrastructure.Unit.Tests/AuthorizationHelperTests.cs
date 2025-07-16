using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizationHelperTests
{
    [Theory]
    [MemberData(nameof(ResolvingScenarios))]
    public async Task CreateDialogSearchAuthorizationResultFromAuthorizedParties_ShouldResolveCorrectly(
        string _, // Used for test explorer readability
        List<string> constraintParties,
        List<string> constraintResources,
        Dictionary<string, List<string>> expectedResourcesByParties,
        List<string> expectedInstanceIds)
    {
        // Arrange
        var authorizedParties = GetAuthorizedParties();

        // Act
        var actualResult = await AuthorizationHelper.CreateDialogSearchAuthorizationResultFromAuthorizedParties(
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
                    Constants.ServiceContextInstanceIdPrefix + "111/instance-1",
                    Constants.ServiceContextInstanceIdPrefix + "222/instance-2"
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
                    Constants.ServiceContextInstanceIdPrefix + "111/instance-1",
                    Constants.ServiceContextInstanceIdPrefix + "222/instance-2"
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
                new List<string> { "urn:altinn:resource:app_org_app-1" },
                new Dictionary<string, List<string>>(),
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/instance-1"
                }
            },

            new object[]
            {
                "With party and instance resource constraint",
                new List<string> { "party1" },
                new List<string> { "urn:altinn:resource:app_org_app-1" },
                new Dictionary<string, List<string>>(),
                new List<string>
                {
                    Constants.ServiceContextInstanceIdPrefix + "111/instance-1"
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
            AuthorizedParties = new List<AuthorizedParty>
            {
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
                        new() { InstanceId = "instance-1", ResourceId = "app_org_app-1" }
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
                        new() { InstanceId = "instance-2", ResourceId = "app_org_app-2" }
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
                        // This one should be ignored as its resource id does not start with app_
                        new() { InstanceId = "instance-4", ResourceId = "xapp_org_app-4" }
                    ]
                }
            }
        };
    }
}
