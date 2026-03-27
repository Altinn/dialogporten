using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetServiceOwnerLabelsSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Get_ServiceOwnerLabels_After_Create_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new() { Value = "initial-label" }
                ]
            };
        });

        // Act - Create a new service owner label
        var createLabelResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabelServiceOwnerLabel(
                dialogId,
                new() { Value = "new-label" },
                null,
                TestContext.Current.CancellationToken);

        createLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act - Get the service owner labels
        var getLabelsResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabelServiceOwnerLabel(
                dialogId,
                TestContext.Current.CancellationToken);

        // Assert
        getLabelsResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        getLabelsResponse.Content.Should().NotBeNull();

        await VerifyJsonSnapshot(
            JsonSerializer.Serialize(
                getLabelsResponse.Content));
    }
}
