using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizationHelperTests
{
    [Theory]
    [MemberData(nameof(CollapseScenarios))]
    public async Task CollapseSubjectResources_ShouldCollapseCorrectly(
        string _, // Used for test explorer readability
        List<string> constraintParties,
        List<string> constraintResources,
        Dictionary<string, List<string>> expectedResult)
    {
        // Arrange
        var authorizedParties = GetAuthorizedParties();

        // Act
        var actualResult = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties,
            constraintParties,
            constraintResources,
            GetSubjectResources,
            CancellationToken.None);

        // Assert
        // 1. Ensure the number of parties in the result is as expected.
        Assert.Equal(expectedResult.Count, actualResult.ResourcesByParties.Count);

        // 2. Iterate through each expected party and its resources for detailed validation.
        foreach (var (expectedParty, expectedResources) in expectedResult)
        {
            // Verify the party is in the actual result
            Assert.Contains(expectedParty, actualResult.ResourcesByParties.Keys);
            var actualResources = actualResult.ResourcesByParties[expectedParty];

            // Verify the resource count for the party is correct
            Assert.Equal(expectedResources.Count, actualResources.Count);

            // Verify that all expected resources are present (order doesn't matter)
            Assert.Empty(expectedResources.Except(actualResources));
        }
    }

    public static IEnumerable<object[]> CollapseScenarios =>
        new List<object[]>
        {
            new object[]
            {
                "No constraints applied",
                new List<string>(),
                new List<string>(),
                new Dictionary<string, List<string>>
                {
                    ["party1"] = new() { "resource1", "resource2", "resource3", "resource4" },
                    ["party2"] = new() { "resource2", "resource3", "resource4" },
                    ["party3"] = new() { "resource5" }
                }
            },

            new object[]
            {
                "With party constraints, without resource constraints",
                new List<string> { "party1", "party2" },
                new List<string>(),
                new Dictionary<string, List<string>>
                {
                    ["party1"] = new() { "resource1", "resource2", "resource3", "resource4" },
                    ["party2"] = new() { "resource2", "resource3", "resource4" }
                }
            },

            new object[]
            {
                "Without party constraints, with resource constraints",
                new List<string>(),
                new List<string> { "resource1", "resource2", "resource5" },
                new Dictionary<string, List<string>>
                {
                    ["party1"] = new() { "resource1", "resource2" },
                    ["party2"] = new() { "resource2" },
                    ["party3"] = new() { "resource5" }
                }
            },

            new object[]
            {
                "With both party and resource constraints",
                new List<string> { "party2" },
                new List<string> { "resource4" },
                new Dictionary<string, List<string>>
                {
                    ["party2"] = new() { "resource4" }
                }
            }
        };


    private static Task<List<SubjectResource>> GetSubjectResources(CancellationToken token)
    {
        return Task.FromResult(new List<SubjectResource>
        {
            new() { Subject = "role1", Resource = "resource1" },
            new() { Subject = "role1", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource3" },
            new() { Subject = "role2", Resource = "resource4" },
            new() { Subject = "role3", Resource = "resource5" }
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
                    Party = "party1",
                    AuthorizedRoles = new List<string> { "role1", "role2" }
                },
                new()
                {
                    Party = "party2",
                    AuthorizedRoles = new List<string> { "role2" }
                },
                new()
                {
                    Party = "party3",
                    AuthorizedRoles = new List<string> { "role3" }
                }
            }
        };
    }
}
