using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizationHelperTests
{
    [Fact]
    public async Task CollapseSubjectResources_ShouldCollapseCorrectly()
    {
        // Arrange
        var dialogSearchAuthorizationResult = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>()
        };
        var authorizedParties = new AuthorizedPartiesResult
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
        var constraintResources = new List<string> { "resource1", "resource2", "resource4" };

        // Simulate subject resources
        var subjectResources = new List<SubjectResource>
        {
            new() { Subject = "role1", Resource = "resource1" },
            new() { Subject = "role1", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource2" },
            new() { Subject = "role2", Resource = "resource3" },
            new() { Subject = "role2", Resource = "resource4" },
            new() { Subject = "role3", Resource = "resource5" }, // Note: not in constraintResources
        };

        Task<List<SubjectResource>> GetSubjectResources(CancellationToken token)
        {
            return Task.FromResult(subjectResources);
        }

        // Act
        dialogSearchAuthorizationResult = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties,
            constraintResources,
            GetSubjectResources,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, dialogSearchAuthorizationResult.ResourcesByParties.Count);
        Assert.Contains("party1", dialogSearchAuthorizationResult.ResourcesByParties.Keys);
        Assert.Contains("resource1", dialogSearchAuthorizationResult.ResourcesByParties["party1"]);
        Assert.Contains("resource2", dialogSearchAuthorizationResult.ResourcesByParties["party1"]);
        Assert.Contains("resource4", dialogSearchAuthorizationResult.ResourcesByParties["party1"]);
        Assert.Equal(3, dialogSearchAuthorizationResult.ResourcesByParties["party1"].Count);

        Assert.Contains("party2", dialogSearchAuthorizationResult.ResourcesByParties.Keys);
        Assert.Contains("resource2", dialogSearchAuthorizationResult.ResourcesByParties["party2"]);
        Assert.Contains("resource4", dialogSearchAuthorizationResult.ResourcesByParties["party2"]);
        Assert.Equal(2, dialogSearchAuthorizationResult.ResourcesByParties["party2"].Count);
    }
}
