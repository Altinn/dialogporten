using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateDialogWithActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Create_Dialog_With_DialogOpened_Activity()
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d =>
            d.AddActivity(a =>
            {
                a.Type = DialogOpened;
                a.PerformedBy = new() { ActorType = Actors_ActorType.ServiceOwner };
            }));

        // Act
        var createDialogResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        var dialogId = createDialogResponse.Content.ToGuid();
        var dialogResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        dialogResponse.Content.Should().NotBeNull();
        dialogResponse.Content.Activities.Should()
            .ContainSingle(a => a.Type == DialogOpened);
    }

    [E2EFact]
    public async Task Should_Reject_Dialog_With_TransmissionOpened_Activity_Without_TransmissionId()
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d =>
            d.AddActivity(a =>
            {
                a.Type = TransmissionOpened;
                a.PerformedBy = new() { ActorType = Actors_ActorType.ServiceOwner };
            }));

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
        response.Error.Content.Should().Contain(nameof(TransmissionOpened));
    }

    [E2EFact]
    public async Task Should_Create_Dialog_With_TransmissionOpened_Activity_Referencing_Transmission()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.AddTransmission(t => t.Id = transmissionId);
            d.AddActivity(a =>
            {
                a.Type = TransmissionOpened;
                a.TransmissionId = transmissionId;
                a.PerformedBy = new() { ActorType = Actors_ActorType.ServiceOwner };
            });
        });

        // Act
        var createDialogResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        var dialogId = createDialogResponse.Content.ToGuid();
        var dialogResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        dialogResponse.Content.Should().NotBeNull();
        dialogResponse.Content.Activities.Should()
            .ContainSingle(a => a.Type == TransmissionOpened);
    }

    [E2EFact]
    public async Task Should_Reject_Dialog_With_DialogOpened_Activity_Referencing_Transmission()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.AddTransmission(t => t.Id = transmissionId);
            d.AddActivity(a =>
            {
                a.Type = DialogOpened;
                a.TransmissionId = transmissionId;
                a.PerformedBy = new() { ActorType = Actors_ActorType.ServiceOwner };
            });
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
    }
}
