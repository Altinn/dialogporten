using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Restore;

[Collection(nameof(WebApiTestCollectionFixture))]
public class RestoreDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Restore_Non_Deleted_Dialog_As_No_Op()
    {
        // Arrange
        var createResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(
                DialogTestData.CreateSimpleDialog(),
                TestContext.Current.CancellationToken);

        createResponse.ShouldHaveStatusCode(HttpStatusCode.Created);
        var dialogId = createResponse.Content.ToGuid();
        var createEtag = createResponse.Headers.ETagToGuid();

        // Act
        var restoreResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsRestoreDialog(
                dialogId,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        restoreResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        restoreResponse.Headers.ETagToGuid().Should().Be(createEtag);
    }

    [E2EFact]
    public async Task Should_Restore_Deleted_Dialog_Preserving_Label_And_UpdatedAt()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(dialog =>
                dialog.SystemLabel = DialogEndUserContextsEntities_SystemLabel.Bin);

        var initialGetResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);
        initialGetResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        initialGetResponse.Content.Should().NotBeNull();
        var initialUpdatedAt = initialGetResponse.Content.UpdatedAt;

        var deleteResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsDeleteDialog(
                dialogId,
                if_Match: null,
                TestContext.Current.CancellationToken);

        deleteResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        var deleteEtag = deleteResponse.Headers.ETagToGuid();

        // Act
        var restoreResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsRestoreDialog(
                dialogId,
                if_Match: deleteEtag,
                TestContext.Current.CancellationToken);

        // Assert
        restoreResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        restoreResponse.Headers.ETagToGuid().Should().NotBe(deleteEtag);

        var restoredGetResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);
        restoredGetResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        restoredGetResponse.Content.Should().NotBeNull();
        restoredGetResponse.Content.EndUserContext.SystemLabels
            .Should().Contain(DialogEndUserContextsEntities_SystemLabel.Bin);
        restoredGetResponse.Content.UpdatedAt.Should().Be(initialUpdatedAt);
    }
}
