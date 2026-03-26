using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.ErrorResponse;

[Collection(nameof(WebApiTestCollectionFixture))]
public class ErrorResponseSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_404_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.ServiceownerApi.GetDialog(nonExistentDialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_Dialog_Creation()
    {
        // Arrange - create a dialog with multiple validation errors
        var invalidDialog = new V1ServiceOwnerDialogsCommandsCreate_Dialog
        {
            ServiceResource = "InvalidServiceResource",
            Party = "InvalidParty",
            Content = new V1ServiceOwnerDialogsCommandsCreate_Content(),
            Progress = 101,
            Process = new string('a', 1024),
            Attachments =
            [
                new()
                {
                    Name = new string('a', 256),
                    DisplayName =
                    [
                        new()
                        {
                            Value = new string('a', 4),
                            LanguageCode = "no"
                        },
                        new()
                        {
                            Value = string.Empty,
                            LanguageCode = "en"
                        }
                    ]
                }
            ]
        };

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(invalidDialog);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task
        Should_Return_410_Error_Response_In_ProblemDetails_Format_When_Creating_Activity_On_Deleted_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);
        var request = DialogTestData.CreateSimpleActivity();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Gone);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_412_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var request = DialogTestData.CreateSimpleActivity();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, Guid.CreateVersion7());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_422_For_Duplicate_Activity()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var activityId = Guid.CreateVersion7();
        var request = DialogTestData.CreateSimpleActivity(activity => activity.Id = activityId);

        await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Act - create the same activity again
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.UnprocessableEntity);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_409_For_Duplicate_Dialog_Id()
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d => d.IdempotentKey = "duplicate-idempotent-key");
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(dialog);

        // Act - create another dialog with the same idempotent key
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(dialog);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    // "This test is flaky. It sometimes fails with a 503 Service Unavailable again the Azure environment,
    // this can also be reproduced locally where you get a HttpRequestException with the error
    // `Error while copying content to a stream` also it tests Kestrel functionality, not Dialogporten"
    [E2EFact(Skip = "Flaky, see comment")]
    public async Task Should_Return_413_For_Payload_Too_Large()
    {
        // Arrange - create a dialog with a body exceeding 20 MB
        var hugeDialog = DialogTestData.CreateSimpleDialog(d =>
            d.Content.Title.Value.First().Value = new string('a', 21_000_000));

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(hugeDialog);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.RequestEntityTooLarge);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_404_Error_Response_For_Other_Orgs_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        using var _ = Fixture.UseServiceOwnerTokenOverrides("964951284", "hko");

        // Act
        var response = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await VerifyJsonSnapshot(response.Error!.Content!);
    }
}
