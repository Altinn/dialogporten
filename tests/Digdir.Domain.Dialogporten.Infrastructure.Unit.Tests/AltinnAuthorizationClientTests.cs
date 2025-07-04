using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AltinnAuthorizationClientTests
{
    [Fact]
    public void GetFlattenedAuthorizedParties_ShouldFlattenHierarchy()
    {
        // Arrange
        var input = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new()
                {
                    Party = "parent",
                    AuthorizedRoles = new List<string> { "role1" },
                    SubParties = new List<AuthorizedParty>
                    {
                        new()
                        {
                            Party = "child1",
                            AuthorizedRoles = new List<string> { "role2" }
                        },
                        new()
                        {
                            Party = "child2",
                            AuthorizedRoles = new List<string>()
                        }
                    }
                },
                new()
                {
                    Party = "independent",
                    AuthorizedRoles = new List<string>()
                }
            }
        };

        // Act
        var method = typeof(AltinnAuthorizationClient).GetMethod(
            "GetFlattenedAuthorizedParties",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = (AuthorizedPartiesResult?)method!.Invoke(null, [input]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.AuthorizedParties.Count);

        var parent = result.AuthorizedParties[0];
        Assert.Equal("parent", parent.Party);
        Assert.Null(parent.ParentParty);
        Assert.Equal(["role1"], parent.AuthorizedRoles);
        Assert.Empty(parent.SubParties!);

        var child1 = result.AuthorizedParties[1];
        Assert.Equal("child1", child1.Party);
        Assert.Equal("parent", child1.ParentParty);
        Assert.Equal(["role2"], child1.AuthorizedRoles);
        Assert.Empty(child1.SubParties!);

        var child2 = result.AuthorizedParties[2];
        Assert.Equal("child2", child2.Party);
        Assert.Equal("parent", child2.ParentParty);
        Assert.Empty(child2.AuthorizedRoles);
        Assert.Empty(child2.SubParties!);

        var independent = result.AuthorizedParties[3];
        Assert.Equal("independent", independent.Party);
        Assert.Null(independent.ParentParty);
        Assert.Empty(independent.AuthorizedRoles);
        Assert.Empty(independent.SubParties!);
    }
}
