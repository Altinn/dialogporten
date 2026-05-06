using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Externals.AltinnAuthorization;

public class AuthorizedPartiesResultTest
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
                    PartyUuid = Guid.NewGuid(),
                    PartyId = 0,
                    Name = "parent",
                    DateOfBirth = null,
                    PartyType = AuthorizedPartyType.Person,
                    IsDeleted = false,
                    HasKeyRole = false,
                    IsCurrentEndUser = false,
                    IsMainAdministrator = false,
                    IsAccessManager = false,
                    HasOnlyAccessToSubParties = false,
                    AuthorizedResources = ["resource1", "resource2"],
                    AuthorizedRolesAndAccessPackages = ["role1"],
                    AuthorizedInstances = [],
                    SubParties =
                    [
                        new()
                        {
                            Party = "child1",
                            PartyUuid = Guid.NewGuid(),
                            PartyId = 0,
                            Name = "child1",
                            DateOfBirth = null,
                            PartyType = AuthorizedPartyType.Person,
                            IsDeleted = false,
                            HasKeyRole = false,
                            IsCurrentEndUser = false,
                            IsMainAdministrator = false,
                            IsAccessManager = false,
                            HasOnlyAccessToSubParties = false,
                            AuthorizedResources = ["resource3"],
                            AuthorizedRolesAndAccessPackages = ["role2"],
                            AuthorizedInstances = [],
                            SubParties = [],
                            ParentParty = "parent"
                        },
                        new()
                        {
                            Party = "child2",
                            PartyUuid = Guid.NewGuid(),
                            PartyId = 0,
                            Name = "child2",
                            DateOfBirth = null,
                            PartyType = AuthorizedPartyType.Person,
                            IsDeleted = false,
                            HasKeyRole = false,
                            IsCurrentEndUser = false,
                            IsMainAdministrator = false,
                            IsAccessManager = false,
                            HasOnlyAccessToSubParties = false,
                            AuthorizedResources = [],
                            AuthorizedRolesAndAccessPackages = [],
                            AuthorizedInstances = [],
                            SubParties = [],
                            ParentParty = "parent"
                        }
                    ],
                    ParentParty = null
                },

                new()
                {
                    Party = "independent",
                    PartyUuid = Guid.NewGuid(),
                    PartyId = 0,
                    Name = "independent",
                    DateOfBirth = null,
                    PartyType = AuthorizedPartyType.Person,
                    IsDeleted = false,
                    HasKeyRole = false,
                    IsCurrentEndUser = false,
                    IsMainAdministrator = false,
                    IsAccessManager = false,
                    HasOnlyAccessToSubParties = false,
                    AuthorizedResources = ["resource4"],
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedInstances = [],
                    SubParties = [],
                    ParentParty = null
                }
            ]
        };

        // Act
        var result = input.Flatten();

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
