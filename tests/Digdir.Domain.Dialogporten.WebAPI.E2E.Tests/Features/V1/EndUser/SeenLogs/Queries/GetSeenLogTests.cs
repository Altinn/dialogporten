using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.SeenLogs.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetSeenLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_SeenLog_By_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var getDialogResponse = await Fixture.EnduserApi.GetDialog(dialogId);
        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResponse.Content.Should().NotBeNull();
        getDialogResponse.Content.SeenSinceLastUpdate.Should().NotBeEmpty();

        var seenLog = getDialogResponse.Content.SeenSinceLastUpdate.FirstOrDefault();
        seenLog.Should().NotBeNull();

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetSeenLogDialogSeenLog(
            dialogId,
            seenLog.Id,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Seen log content was null.");
        content.Id.Should().Be(seenLog.Id);
    }
}
