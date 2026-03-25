using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ServiceOwnerSystemLabel = Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_BulkSet_Labels_For_Accessible_Dialogs()
    {
        // Arrange
        var dialogId1 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogId2 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabelsBulkSetDialogSystemLabels(
                E2EConstants.DefaultParty,
                new V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel
                {
                    Dialogs =
                    [
                        new() { DialogId = dialogId1 },
                        new() { DialogId = dialogId2 }
                    ],
                    AddLabels = [ServiceOwnerSystemLabel.Bin]
                },
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var dialog1Response = await Fixture.ServiceownerApi.GetDialog(dialogId1, E2EConstants.DefaultParty);
        var dialog2Response = await Fixture.ServiceownerApi.GetDialog(dialogId2, E2EConstants.DefaultParty);

        dialog1Response.ShouldHaveStatusCode(HttpStatusCode.OK);
        dialog2Response.ShouldHaveStatusCode(HttpStatusCode.OK);

        var dialog1 = dialog1Response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        var dialog2 = dialog2Response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        dialog1.EndUserContext.SystemLabels.Should().Contain(ServiceOwnerSystemLabel.Bin);
        dialog2.EndUserContext.SystemLabels.Should().Contain(ServiceOwnerSystemLabel.Bin);
    }

    [E2EFact]
    public async Task Should_Set_Label_As_ServiceOwner()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Bin]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var dialogResponse = await Fixture.ServiceownerApi.GetDialog(dialogId, E2EConstants.DefaultParty);
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var dialog = dialogResponse.Content ?? throw new InvalidOperationException("Dialog content was null.");
        dialog.EndUserContext.SystemLabels.Should().Contain(ServiceOwnerSystemLabel.Bin);
    }

    [E2EFact]
    public async Task Should_Accept_Multiple_SystemLabels()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Bin, ServiceOwnerSystemLabel.Archive]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
    }

    [E2EFact]
    public async Task Should_Return_412_For_Invalid_IfMatch_Revision()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Bin],
                revision: Guid.NewGuid());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unauthorized_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{E2EConstants.GetDefaultServiceOwnerOrgNr()}");

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Archive]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }
}
