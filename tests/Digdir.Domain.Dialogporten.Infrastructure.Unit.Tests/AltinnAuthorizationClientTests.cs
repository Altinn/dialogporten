using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AltinnAuthorizationClientTests
{
    [Fact]
    public void InstanceRef_ToLookupLabel_Should_Map_AppInstanceRef_To_StorageLabel()
    {
        var instanceId = Guid.NewGuid();
        var rawInstanceRef = $"urn:altinn:instance-id:1337/{instanceId}";

        var parsed = InstanceRef.TryParse(rawInstanceRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(
            $"urn:altinn:integration:storage:1337/{instanceId}",
            instanceRef!.Value.ToLookupLabel());
    }

    [Fact]
    public void InstanceRef_ToLookupLabel_Should_Keep_CorrespondenceRef_Unchanged()
    {
        var correspondenceRef = $"urn:altinn:correspondence-id:{Guid.NewGuid()}";
        var parsed = InstanceRef.TryParse(correspondenceRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(correspondenceRef, instanceRef!.Value.ToLookupLabel());
    }

    [Fact]
    public void InstanceRef_Should_Parse_DialogRef()
    {
        var dialogId = Guid.NewGuid();
        var dialogRef = $"urn:altinn:dialog-id:{dialogId}";

        var parsed = InstanceRef.TryParse(dialogRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(InstanceRefType.DialogId, instanceRef!.Value.Type);
        Assert.Equal(dialogId, instanceRef.Value.Id);
    }

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
