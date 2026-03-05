using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Add_One_LabelLog_Entry_After_Setting_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelRequest = CreateSetLabelRequest(DialogEndUserContextsEntities_SystemLabel.Bin);

        var setLabelResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                setLabelRequest,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        setLabelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var labelLogResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                TestContext.Current.CancellationToken);

        labelLogResponse.IsSuccessful.Should().BeTrue();
        var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
        labelLog.Should().HaveCount(1);
    }

    [E2EFact]
    public async Task Should_Add_Two_LabelLog_Entries_When_Changing_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act - First set Bin label
        var firstSetLabelRequest = CreateSetLabelRequest(DialogEndUserContextsEntities_SystemLabel.Bin);

        var firstSetLabelResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                firstSetLabelRequest,
                if_Match: null,
                TestContext.Current.CancellationToken);

        firstSetLabelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Then change to Archive label
        var secondSetLabelRequest = CreateSetLabelRequest(DialogEndUserContextsEntities_SystemLabel.Archive);

        var secondSetLabelResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                secondSetLabelRequest,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        secondSetLabelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var labelLogResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                TestContext.Current.CancellationToken);

        labelLogResponse.IsSuccessful.Should().BeTrue();
        var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
        labelLog.Should().HaveCount(3);
    }

    [E2EFact]
    public async Task Should_Accept_Multiple_Labels()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelRequest = CreateSetLabelRequest(
            DialogEndUserContextsEntities_SystemLabel.Bin,
            DialogEndUserContextsEntities_SystemLabel.Archive);

        var setLabelResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                setLabelRequest,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        setLabelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [E2EFact]
    public async Task Should_Return_412_For_Invalid_IfMatch_Revision()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelRequest = CreateSetLabelRequest(DialogEndUserContextsEntities_SystemLabel.Bin);

        var invalidRevision = Guid.NewGuid();

        var setLabelResponse = await Fixture.EnduserApi
            .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                setLabelRequest,
                if_Match: invalidRevision,
                TestContext.Current.CancellationToken);

        // Assert
        setLabelResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    // [E2EFact]
    // public async Task Should_Add_LabelLog_Entry_After_ServiceOwner_Updates_Dialog()
    // {
    //     // Arrange
    //     var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
    //
    //     // Set Bin label
    //     var firstSetLabelRequest = new V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest
    //     {
    //         AddLabels = [DialogEndUserContextsEntities_SystemLabel.Bin]
    //     };
    //
    //     await Fixture.EnduserApi
    //         .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
    //             dialogId,
    //             firstSetLabelRequest,
    //             if_Match: null,
    //             TestContext.Current.CancellationToken);
    //
    //     // Set Archive label
    //     var secondSetLabelRequest = new V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest
    //     {
    //         AddLabels = [DialogEndUserContextsEntities_SystemLabel.Archive]
    //     };
    //
    //     await Fixture.EnduserApi
    //         .V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
    //             dialogId,
    //             secondSetLabelRequest,
    //             if_Match: null,
    //             TestContext.Current.CancellationToken);
    //
    //     // Act - Service owner updates dialog
    //     var updateDialogDto = new V1ServiceOwnerDialogsCommandsUpdate_Dialog
    //     {
    //         Progress = 60
    //     };
    //
    //     var updateResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsUpdateDialog(
    //         dialogId,
    //         updateDialogDto,
    //         if_Match: null,
    //         TestContext.Current.CancellationToken);
    //
    //     // Assert
    //     updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    //
    //     var labelLogResponse = await Fixture.EnduserApi
    //         .V1EndUserEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
    //             dialogId,
    //             TestContext.Current.CancellationToken);
    //
    //     labelLogResponse.IsSuccessful.Should().BeTrue();
    //     var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
    //     labelLog.Should().HaveCount(4);
    // }

    private static V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest CreateSetLabelRequest(
        params DialogEndUserContextsEntities_SystemLabel[] labels) =>
        new() { AddLabels = [.. labels] };
}
