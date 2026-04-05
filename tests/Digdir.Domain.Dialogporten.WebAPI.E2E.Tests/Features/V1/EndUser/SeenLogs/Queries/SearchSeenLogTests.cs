using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.SeenLogs.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchSeenLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Search_SeenLogs()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var getDialogResponse = await Fixture.EnduserApi.GetDialog(dialogId);
        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var dialogContent = getDialogResponse.Content ?? throw new InvalidOperationException("Dialog content was null.");
        var seenLogId = dialogContent.SeenSinceLastUpdate.Single().Id;

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchSeenLogsDialogSeenLog(
            dialogId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Seen log content was null.");
        content.Should().ContainSingle().Which.Id.Should().Be(seenLogId);
    }

    [E2EFact]
    public async Task Should_Verify_SearchSeenLog_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger seen log
        var getDialogResponse1 = await Fixture.EnduserApi.GetDialog(dialogId);
        getDialogResponse1.ShouldHaveStatusCode(HttpStatusCode.OK);

        var patchResponse = await Fixture.ServiceownerApi
            .PatchDialogAsync(
                dialogId,
                ops => ops.Add(new()
                {
                    Op = "replace",
                    Path = "/progress",
                    Value = 69
                }));
        patchResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Trigger seen log
        var getDialogResponse2 = await Fixture.ServiceownerApi
            .GetDialog(dialogId, endUserId: E2EConstants.DefaultParty);
        getDialogResponse2.ShouldHaveStatusCode(HttpStatusCode.OK);

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchSeenLogsDialogSeenLog(
            dialogId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        await VerifyJsonSnapshot(JsonSerializer.Serialize(response.Content));
    }
}
