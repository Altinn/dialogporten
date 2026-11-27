using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchLabelAssignmentLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Label_Assignment_Log_On_Behalf_Of_End_User()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var setLabelResponse = await Fixture.ServiceownerApi
            .SetSystemLabel(dialogId, E2EConstants.DefaultParty, request => request.AddLabels = [Bin]);
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                E2EConstants.DefaultParty,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        var log = response.Content.Should().ContainSingle().Subject;
        log.Name.Should().Be("systemlabel:Bin");
        log.Action.Should().Be("set");
        log.PerformedBy.ActorId.Should().Be(E2EConstants.DefaultParty);
    }

    [E2EFact]
    public async Task Should_Get_Label_Assignment_Log_Without_End_User()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var setLabelResponse = await Fixture.ServiceownerApi
            .SetSystemLabel(dialogId, E2EConstants.DefaultParty, request => request.AddLabels = [Archive]);
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        var log = response.Content.Should().ContainSingle().Subject;

        log.Name.Should().Be("systemlabel:Archive");
        log.Action.Should().Be("set");
        log.PerformedBy.ActorId.Should().Be(E2EConstants.DefaultParty);
    }

    [E2EFact]
    public async Task Should_Return_Empty_List_When_No_Labels_Have_Been_Changed()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                E2EConstants.DefaultParty,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Should().BeEmpty();
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unknown_Dialog()
    {
        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                Guid.CreateVersion7(),
                E2EConstants.DefaultParty,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_404_When_End_User_Has_No_Access()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                E2EConstants.AlternateParty,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_404_When_Token_Has_Unauthorized_Org()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        Fixture.UseServiceOwnerTokenOverrides("964951284", "hko");
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                cancellationToken: TestContext.Current.CancellationToken);
        Fixture.UseServiceOwnerTokenOverrides();

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_End_User_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                "not-a-valid-enduser-id",
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Return_410_For_Deleted_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var deleteResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null, TestContext.Current.CancellationToken);
        deleteResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                E2EConstants.DefaultParty,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Gone);
    }
}
