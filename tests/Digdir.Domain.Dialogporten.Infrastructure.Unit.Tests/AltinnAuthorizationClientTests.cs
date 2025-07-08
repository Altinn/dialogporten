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
            AuthorizedParties =
            [
                new()
                {
                    Party = "parent",
                    AuthorizedRolesAndAccessPackages = ["role1"],
                    AuthorizedResources = ["resource1", "resource2"],
                    SubParties =
                    [
                        new()
                        {
                            Party = "child1",
                            AuthorizedRolesAndAccessPackages = ["role2"],
                            AuthorizedResources = ["resource3"],
                        },

                        new()
                        {
                            Party = "child2",
                            AuthorizedRolesAndAccessPackages = []
                        }
                    ]
                },

                new()
                {
                    Party = "independent",
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedResources = ["resource4"],
                }
            ]
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
        Assert.Equal(["role1"], parent.AuthorizedRolesAndAccessPackages);
        Assert.Equal(["resource1", "resource2"], parent.AuthorizedResources);
        Assert.Empty(parent.SubParties!);

        var child1 = result.AuthorizedParties[1];
        Assert.Equal("child1", child1.Party);
        Assert.Equal("parent", child1.ParentParty);
        Assert.Equal(["role2"], child1.AuthorizedRolesAndAccessPackages);
        Assert.Equal(["resource3"], child1.AuthorizedResources);
        Assert.Empty(child1.SubParties!);

        var child2 = result.AuthorizedParties[2];
        Assert.Equal("child2", child2.Party);
        Assert.Equal("parent", child2.ParentParty);
        Assert.Equal([], child2.AuthorizedResources);
        Assert.Empty(child2.AuthorizedRolesAndAccessPackages);
        Assert.Empty(child2.SubParties!);

        var independent = result.AuthorizedParties[3];
        Assert.Equal("independent", independent.Party);
        Assert.Null(independent.ParentParty);
        Assert.Empty(independent.AuthorizedRolesAndAccessPackages);
        Assert.Empty(independent.SubParties!);
        Assert.Equal(["resource4"], independent.AuthorizedResources);
    }
}
