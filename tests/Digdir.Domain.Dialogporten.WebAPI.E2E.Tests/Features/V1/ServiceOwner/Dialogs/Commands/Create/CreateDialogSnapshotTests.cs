using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ServiceOwnerSystemLabel = Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateDialogSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Create_Simple_Dialog_Then_Get_Verify_Snapshot()
    {
        // Arrange, Act
        var createDialogResult = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(new()
            {
                ServiceResource = E2EConstants.DefaultServiceResource,
                Party = E2EConstants.DefaultParty,
                Content = new()
                {
                    Title = new()
                    {
                        Value = [new()
                        {
                            LanguageCode = "en",
                            Value = "Create dialog title"
                        }]
                    }
                }
            });

        createDialogResult.ShouldHaveStatusCode(HttpStatusCode.Created);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(createDialogResult.Content),
            fileNameSuffix: "created-response");

        var dialogId = createDialogResult.Content.ToGuid();

        var getDialogResult = await Fixture.ServiceownerApi.GetDialog(dialogId, E2EConstants.DefaultParty);

        // Assert
        getDialogResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResult.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Create_Complex_Dialog_Then_Get_Verify_Snapshot()
    {
        // Arrange
        var createDialogCommand = DialogTestData.CreateComplexDialog(ConfigureComplexCreateDialogSnapshotData);

        // Act
        var createDialogResult = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(createDialogCommand);
        createDialogResult.ShouldHaveStatusCode(HttpStatusCode.Created);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(createDialogResult.Content),
            fileNameSuffix: "created-response");

        var dialogId = createDialogResult.Content.ToGuid();

        var getDialogResult = await Fixture.ServiceownerApi.GetDialog(dialogId, E2EConstants.DefaultParty);

        // Assert
        getDialogResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResult.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }

    private static void ConfigureComplexCreateDialogSnapshotData(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
    {
        dialog.SystemLabel = ServiceOwnerSystemLabel.Archive;
        dialog.ServiceOwnerContext = new()
        {
            ServiceOwnerLabels =
            [
                new() { Value = "create-dialog-primary-label" },
                new() { Value = "create-dialog-secondary-label" }
            ]
        };
        dialog.SearchTags =
        [
            new() { Value = "create-dialog-search-primary" },
            new() { Value = "create-dialog-search-secondary" }
        ];
        dialog.Content.MainContentReference = DialogTestData.CreateContentValue(
            value: "https://altinn.no/create-dialog-main-content",
            languageCode: "nb",
            mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown");
        dialog.Attachments =
        [
            new()
            {
                Name = "create-dialog-attachment",
                DisplayName =
                [
                    DialogTestData.CreateLocalization("Create dialog attachment", "en"),
                    DialogTestData.CreateLocalization("Create dialog vedlegg")
                ],
                Urls =
                [
                    new()
                    {
                        Url = new("https://altinn.no/create-dialog-attachment.pdf"),
                        MediaType = "application/pdf",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    },
                    new()
                    {
                        Url = new("https://altinn.no/create-dialog-attachment-api.json"),
                        MediaType = "application/json",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Api
                    }
                ],
                ExpiresAt = new DateTimeOffset(2037, 1, 2, 3, 4, 5, TimeSpan.Zero)
            }
        ];

        TransmissionTestData.AddComplexTransmissions(dialog);
        if (dialog.Transmissions?.FirstOrDefault() is { } firstTransmission)
        {
            firstTransmission.AuthorizationAttribute = E2EConstants.AvailableExternalResource;
            firstTransmission.Content.ContentReference = DialogTestData.CreateContentValue(
                value: "https://altinn.no/create-dialog-transmission-content",
                languageCode: "nb",
                mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown");
        }

        if (dialog.Transmissions?.Skip(1).FirstOrDefault() is { } secondTransmission)
        {
            secondTransmission.AuthorizationAttribute = E2EConstants.UnavailableSubresource;
            secondTransmission.Type = DialogsEntitiesTransmissions_DialogTransmissionType.Submission;
            secondTransmission.NavigationalActions =
            [
                new()
                {
                    Title =
                    [
                        DialogTestData.CreateLocalization("Create dialog transmission navigation")
                    ],
                    Url = new("https://altinn.no/create-dialog-transmission-navigation"),
                    ExpiresAt = new DateTimeOffset(2038, 1, 2, 3, 4, 5, TimeSpan.Zero)
                }
            ];

            if (secondTransmission.Attachments?.FirstOrDefault() is { } transmissionAttachment)
            {
                transmissionAttachment.Urls =
                [
                    new()
                    {
                        Url = new("https://altinn.no/create-dialog-transmission-attachment.pdf"),
                        MediaType = "application/pdf",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    },
                    new()
                    {
                        Url = new("https://altinn.no/create-dialog-transmission-attachment-api.json"),
                        MediaType = "application/json",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Api
                    }
                ];
            }
        }

        if (dialog.GuiActions?.FirstOrDefault() is { } guiAction)
        {
            guiAction.AuthorizationAttribute = E2EConstants.AvailableExternalResource;
        }

        if (dialog.ApiActions?.FirstOrDefault() is { } apiAction)
        {
            apiAction.AuthorizationAttribute = E2EConstants.AvailableExternalResource;
        }
    }
}
