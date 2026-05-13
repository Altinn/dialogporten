using System.Net;
using System.Text.Json;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.SeenLogs.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchSeenLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Verify_SearchSeenLog_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger seen log once with EndUser
        var getDialogResponse = await Fixture.EndUserApi.V1.GetDialog(dialogId, new());

        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var patchResponse = await Fixture.ServiceownerApi.PatchDialogAsync(dialogId, ops => ops.Add(new()
        {
            Path = "/status",
            Op = "replace",
            Value = "Awaiting"
        }));

        patchResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Trigger new seen log after patch with ServiceOwner representing EndUser
        var getDialogAfterPatchResponse = await Fixture.ServiceownerApi.GetDialog(
            dialogId,
            E2EConstants.DefaultParty);

        getDialogAfterPatchResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var seenLogResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchSeenLogsDialogSeenLog(dialogId);
        seenLogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        // Assert
        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(seenLogResponse.Content));
    }
}
