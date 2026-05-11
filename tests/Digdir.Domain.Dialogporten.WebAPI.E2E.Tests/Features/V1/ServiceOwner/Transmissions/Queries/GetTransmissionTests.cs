using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Transmissions.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetTransmissionTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Get_Transmission_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(
            TransmissionTestData.AddComplexTransmissions);

        var dialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        dialog.Content.Should().NotBeNull();

        var transmissionId = dialog.Content.Transmissions
            .Single(t => t.RelatedTransmissionId is not null).Id;

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetTransmissionDialogTransmission(
            dialogId,
            transmissionId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
