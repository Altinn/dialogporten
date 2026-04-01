using System.Text.Json;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.ServiceOwner.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(WebApiServiceOwnerTestCollectionFixture))]
public class GetDialogTests(WebApiServiceOwnerE2EFixture fixture) :
    E2ETestBase<WebApiServiceOwnerE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Get_Dialog_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceOwnerClient.V1.CreateComplexDialogAsync();

        // Act
        var getDialogResult = await Fixture
            .ServiceOwnerClient.V1
            .GetDialog(
                dialogId,
                cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content),
            fileNameSuffix: Fixture.DotnetEnvironment);
    }
}
