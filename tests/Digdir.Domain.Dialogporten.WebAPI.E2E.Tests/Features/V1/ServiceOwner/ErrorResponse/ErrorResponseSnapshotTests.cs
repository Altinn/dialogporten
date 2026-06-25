using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_Dialog_Creation()
    {
        // Arrange - create a dialog with multiple validation errors
        var invalidDialog = new V1ServiceOwnerDialogsCommandsCreate_Dialog
        {
            ServiceResource = "InvalidServiceResource",
            Party = "InvalidParty",
            Content = new(),
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    // "This test is flaky. It sometimes fails with a 503 Service Unavailable in the Azure environments,
    // and can be reproduced locally where you get an HttpRequestException with the error
    // `Error while copying content to a stream`.
    // It also tests Kestrel functionality, not Dialogporten"
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
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
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_Dialog_Update()
    {
        // Arrange - update a dialog with multiple validation errors
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var invalidUpdateDialog = new V1ServiceOwnerDialogsCommandsUpdate_Dialog
        {
            Content = new(),
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
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, invalidUpdateDialog, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_404_For_Update_On_Non_Existent_Dialog()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(nonExistentDialogId, CreateSimpleUpdateDialog(), null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_410_For_Update_On_Deleted_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, CreateSimpleUpdateDialog(), null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Gone);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_412_For_Update_With_Invalid_Revision()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act - update with an if-match revision that does not match the dialog's revision
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, CreateSimpleUpdateDialog(), Guid.CreateVersion7());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_403_For_Update_With_Correspondence_Activity_Without_Scope()
    {
        // Arrange - correspondence activity types require a scope the default test token lacks
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var updateDialog = CreateSimpleUpdateDialog(dialog => dialog.Activities =
        [
            new()
            {
                Type = DialogsEntitiesActivities_DialogActivityType.CorrespondenceConfirmed,
                PerformedBy = new()
                {
                    ActorType = Actors_ActorType.ServiceOwner
                }
            }
        ]);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, updateDialog, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_422_For_Update_With_Duplicate_Activity_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var activityId = Guid.CreateVersion7();
        await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId, modify: activity => activity.Id = activityId);

        var updateDialog = CreateSimpleUpdateDialog(dialog => dialog.Activities =
        [
            new()
            {
                Id = activityId,
                Type = DialogsEntitiesActivities_DialogActivityType.Information,
                PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.ServiceOwner
                },
                Description = [DialogTestData.CreateLocalization("En beskrivelse")]
            }
        ]);

        // Act - append an activity with an ID that already exists on the dialog
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, updateDialog, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.UnprocessableEntity);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    [E2EFact]
    public async Task Should_Return_409_For_Update_With_Duplicate_Transmission_Idempotent_Key()
    {
        // Arrange
        const string idempotentKey = "duplicate-transmission-key";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.AddTransmission(transmission => transmission.IdempotentKey = idempotentKey));

        var updateDialog = CreateSimpleUpdateDialog(dialog => dialog.Transmissions =
        [
            new()
            {
                IdempotentKey = idempotentKey,
                Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                Sender = new()
                {
                    ActorType = Actors_ActorType.ServiceOwner
                },
                Content = new()
                {
                    Title = DialogTestData.CreateContentValue(
                        value: "Ny forsendelse",
                        languageCode: "nb")
                }
            }
        ]);

        // Act - append a transmission with an idempotent key that already exists on the dialog
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, updateDialog, null);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
        await response.VerifyProblemDetailsSnapshot<ProblemDetails>();
    }

    private static V1ServiceOwnerDialogsCommandsUpdate_Dialog CreateSimpleUpdateDialog(
        Action<V1ServiceOwnerDialogsCommandsUpdate_Dialog>? modify = null)
    {
        var updateDialog = new V1ServiceOwnerDialogsCommandsUpdate_Dialog
        {
            Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.InProgress,
            Content = new()
            {
                Title = DialogTestData.CreateContentValue(
                    value: "Oppdatert skjema for rapportering av et eller annet",
                    languageCode: "nb"),
                Summary = DialogTestData.CreateContentValue(
                    value: "Et oppdatert sammendrag her.",
                    languageCode: "nb")
            }
        };

        modify?.Invoke(updateDialog);
        return updateDialog;
    }
}
