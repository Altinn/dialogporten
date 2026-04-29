using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Refit;
using static Altinn.ApiClients.Dialogporten.EndUser.Features.V1.SystemLabel;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchLabelAssignmentLogSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Label_Assignment_Log_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        await SetLabel(dialogId, Bin);
        await SetLabel(dialogId, Archive);
        await SetLabel(dialogId, Default);

        // Act
        var response = await Fixture.EnduserApi.GetSystemLabelAssignmentLog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();

        await VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }

    private Task<IApiResponse> SetLabel(Guid dialogId,
        SystemLabel label) =>
        Fixture.EnduserApi.SetSystemLabels(
            dialogId,
            request => request.AddLabels = [label]);
}
