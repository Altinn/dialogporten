using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Transmissions.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetTransmissionTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Transmission_By_Id()
    {
        // Arrange
        var transmissionId = Guid.CreateVersion7();

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.AddTransmission(transmission => transmission.Id = transmissionId));

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetTransmissionDialogTransmission(
            dialogId,
            transmissionId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Transmission content was null.");
        content.Id.Should().Be(transmissionId);
    }

    [E2EFact]
    public async Task Get_Transmission_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(
            TransmissionTestData.AddComplexTransmissions);

        var dialog = await Fixture.EnduserApi.GetDialog(dialogId);
        dialog.Content.Should().NotBeNull();

        var transmissionId = dialog.Content.Transmissions
            .Single(t => t.RelatedTransmissionId is not null).Id;

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetTransmissionDialogTransmission(
            dialogId,
            transmissionId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
