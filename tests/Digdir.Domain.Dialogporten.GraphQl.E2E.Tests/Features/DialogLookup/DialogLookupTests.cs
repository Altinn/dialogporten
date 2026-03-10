using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using AwesomeAssertions;
using StrawberryShake;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogLookup;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class DialogLookupTests : E2ETestBase<GraphQlE2EFixture>
{
    public DialogLookupTests(GraphQlE2EFixture fixture) : base(fixture) { }

    [E2EFact]
    public async Task Should_Return_Typed_NotFound_Error_For_Unknown_InstanceRef()
    {
        // Arrange
        var instanceRef = $"urn:altinn:instance-id:1337/{Guid.NewGuid()}";

        // Act
        var result = await LookupDialog(instanceRef);

        // Assert
        result.Data.Should().NotBeNull();

        var error = result.Data.DialogLookup.Errors.Single();

        error.Should().BeOfType<GetDialogLookup_DialogLookup_Errors_DialogLookupNotFound>();
        error.Message.Should().Contain(instanceRef);
    }

    [E2EFact]
    public async Task Should_Return_Lookup_For_Existing_InstanceRef()
    {
        // Arrange
        var instanceRef = $"urn:altinn:instance-id:1337/{Guid.NewGuid()}";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel
                    {
                        Value = instanceRef
                    }
                ]
            };
        });

        // Act
        var result = await LookupDialog(instanceRef);

        // Assert
        result.Data.Should().NotBeNull();

        var lookup = result.Data.DialogLookup.Lookup;
        lookup.Should().NotBeNull();
        lookup!.DialogId.Should().Be(dialogId);
        lookup.InstanceRef.Should().Be(instanceRef.ToLowerInvariant());
        lookup.ServiceResource.Id.Should().NotBeNullOrWhiteSpace();
        lookup.ServiceOwner.Code.Should().NotBeNullOrWhiteSpace();
    }

    [E2EFact]
    public async Task Should_Return_401_Unauthorized_With_Invalid_EndUser_Token()
    {
        // Arrange
        using var _ = Fixture.UseEndUserTokenOverrides(tokenOverride: "invalid.jwt.token");

        // Act
        var result = await LookupDialog($"urn:altinn:instance-id:1337/{Guid.NewGuid()}");

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("401 (Unauthorized)");
    }

    private Task<IOperationResult<IGetDialogLookupResult>> LookupDialog(string instanceRef) =>
        Fixture.GraphQlClient.GetDialogLookup.ExecuteAsync(instanceRef, TestContext.Current.CancellationToken);
}
