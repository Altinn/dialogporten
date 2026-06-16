using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.SeenLogs.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetSeenLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_SeenLog_By_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var getDialogResponse = await Fixture.EndUserApi.GetDialog(
            dialogId,
            cancellationToken: TestContext.Current.CancellationToken);

        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResponse.Content.Should().NotBeNull();
        var seenLog = getDialogResponse.Content
            .SeenSinceLastUpdate.FirstOrDefault();

        seenLog.Should().NotBeNull();

        // Act
        var response = await Fixture.EndUserApi.V1.GetDialogSeenLog(
            dialogId,
            seenLog.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Id.Should().Be(seenLog.Id);
    }

    [E2EFact]
    public async Task Should_Verify_GetSeenLog_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger a seen log as the current end user
        var getDialogResponse = await Fixture.EndUserApi.GetDialog(
            dialogId, cancellationToken: TestContext.Current.CancellationToken);
        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        getDialogResponse.Content.Should().NotBeNull();
        var seenLogId = getDialogResponse.Content.SeenSinceLastUpdate.Single().Id;

        // Act
        var response = await Fixture.EndUserApi.V1.GetDialogSeenLog(
            dialogId,
            seenLogId,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        await VerifyJsonSnapshot(JsonSerializer.Serialize(response.Content));
    }

    [E2EFact]
    public async Task Should_Verify_GetSeenLog_ViaServiceOwner_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger a seen log via the service owner (on behalf of the end user)
        var getDialogResponse = await Fixture.ServiceownerApi
            .GetDialog(dialogId, endUserId: E2EConstants.DefaultParty);
        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        getDialogResponse.Content.Should().NotBeNull();
        var seenLogId = getDialogResponse.Content.SeenSinceLastUpdate.Single().Id;

        // Act
        var response = await Fixture.EndUserApi.V1.GetDialogSeenLog(
            dialogId,
            seenLogId,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        await VerifyJsonSnapshot(JsonSerializer.Serialize(response.Content));
    }
}
