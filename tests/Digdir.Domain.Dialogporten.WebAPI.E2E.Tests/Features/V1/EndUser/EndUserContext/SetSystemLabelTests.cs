using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.EndUser.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Set_System_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.EnduserApi
            .SetSystemLabels(dialogId, request => request.AddLabels = [Bin]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
    }

    [E2EFact]
    public async Task Should_Add_One_LabelLog_Entry_After_Setting_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelResponse = await Fixture.EnduserApi
            .SetSystemLabels(dialogId, request => request.AddLabels = [Bin]);

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var labelLogResponse = await Fixture.EnduserApi
            .GetSystemLabelAssignmentLog(dialogId);

        labelLogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
        labelLog.Should().HaveCount(1);
        labelLog.Should().ContainSingle(x => x.Action == "set")
            .Which.PerformedBy.ActorType.Should()
            .Be(Actors_ActorType.PartyRepresentative);
    }

    [E2EFact]
    public async Task Should_Add_Three_LabelLog_Entries_When_Changing_Label_Twice()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act - First set Bin label
        var firstSetLabelResponse = await Fixture.EnduserApi
            .SetSystemLabels(
                dialogId,
                request => request.AddLabels = [Bin]);

        firstSetLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act - Then change to Archive label
        var secondSetLabelResponse = await Fixture.EnduserApi
            .SetSystemLabels(
                dialogId,
                request => request.AddLabels = [Archive]);

        // Assert
        secondSetLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var labelLogResponse = await Fixture.EnduserApi
            .GetSystemLabelAssignmentLog(dialogId);

        labelLogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
        labelLog.Should().HaveCount(3);

        labelLog.Should().AllSatisfy(x =>
                x.PerformedBy.Should().NotBeNull())
            .And.AllSatisfy(x => x.PerformedBy.ActorType
                .Should().Be(Actors_ActorType.PartyRepresentative));

        labelLog.Where(x => x.Action == "set").Should().HaveCount(2);
        labelLog.Should().ContainSingle(x => x.Action == "remove");
    }

    [E2EFact]
    public async Task Should_Accept_Multiple_Labels()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelResponse = await Fixture.EnduserApi
            .SetSystemLabels(
                dialogId,
                request => request.AddLabels = [Bin, Archive]);

        var dialogResponse = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        // Last label is selected when multiple of Default/Bin/Archive is supplied
        dialogResponse.Content!.EndUserContext.SystemLabels.Should()
            .ContainSingle(x => x == Archive);
    }

    [E2EFact]
    public async Task Should_Return_412_For_Invalid_IfMatch_Revision()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var invalidRevision = Guid.NewGuid();

        var setLabelResponse = await Fixture.EnduserApi
            .SetSystemLabels(
                dialogId,
                request => request.AddLabels = [Bin],
                revision: invalidRevision);

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
    }

    [E2EFact]
    public async Task Should_Add_LabelLog_Entry_After_ServiceOwner_Updates_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(x => x.Progress = 68);

        // Set Archive label
        await Fixture.EnduserApi
            .SetSystemLabels(
                dialogId,
                request => request.AddLabels = [Archive]);

        // Act - Service owner patches dialog
        var patchResponse = await Fixture.ServiceownerApi
            .PatchDialogAsync(
                dialogId,
                ops => ops.Add(new()
                {
                    Op = "replace",
                    Path = "/progress",
                    Value = 69
                }));

        // Assert
        patchResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var labelLogResponse = await Fixture.EnduserApi
            .GetSystemLabelAssignmentLog(dialogId);

        labelLogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var labelLog = labelLogResponse.Content ?? throw new InvalidOperationException("Label log content was null.");
        labelLog.Should().HaveCount(2);

        labelLog.Should().ContainSingle(x => x.Action == "set")
            .Which.PerformedBy.ActorType.Should()
            .Be(Actors_ActorType.PartyRepresentative);

        labelLog.Should().ContainSingle(x => x.Action == "remove")
            .Which.PerformedBy.ActorType.Should()
            .Be(Actors_ActorType.ServiceOwner);
    }
}
