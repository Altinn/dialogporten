using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.SeenLogs.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetSeenLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Get_SeenLog_By_Id_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger seen log by fetching the dialog as the end user, retrying
        // until the seen log entry has propagated.
        var getDialogResponse = await E2ERetryPolicies.RetryUntilAsync(
            operation: ct => Fixture.EndUserApi.GetDialog(dialogId, cancellationToken: ct),
            isSuccessful: result => result is { IsSuccessful: true, Content.SeenSinceLastUpdate.Count: 1 },
            degradationMessage: "Seen log creation delayed");

        getDialogResponse.Content.Should().NotBeNull();
        var seenLog = getDialogResponse.Content.SeenSinceLastUpdate.FirstOrDefault();
        seenLog.Should().NotBeNull();

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetSeenLogDialogSeenLog(
            dialogId,
            seenLog.Id,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Id.Should().Be(seenLog.Id);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
