using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Activities.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Activity_By_Id_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId, null, x =>
        {
            x.Type = Information;
            x.Description =
            [
                new()
                {
                    LanguageCode = "en",
                    Value = "Test activity"
                }
            ];
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetActivityDialogActivity(
            dialogId,
            activityId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Id.Should().Be(activityId);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }

    [E2EFact]
    public async Task Should_Get_TransmissionOpened_Activity_By_Id_Verify_Snapshot()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(t => t.Id = transmissionId));

        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId, null, x =>
        {
            x.Type = TransmissionOpened;
            x.TransmissionId = transmissionId;
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetActivityDialogActivity(
            dialogId,
            activityId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();

        var content = response.Content;
        content.Id.Should().Be(activityId);
        content.TransmissionId.Should().Be(transmissionId);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
