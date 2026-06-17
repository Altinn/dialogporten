using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ServiceOwnerSystemLabel = Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogTests : E2ETestBase<WebApiE2EFixture>
{
    public GetDialogTests(WebApiE2EFixture fixture) : base(fixture) { }

    [E2EFact]
    public async Task Should_Get_Dialog_By_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Id.Should().Be(dialogId);
    }

    [E2EFact]
    public async Task Should_Return_Attachment_Names()
    {
        // Arrange
        const string dialogAttachmentName = "dialog-attachment";
        const string transmissionAttachmentName = "transmission-attachment";

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.AddAttachment(x =>
                x.Name = dialogAttachmentName);
            dialog.AddTransmission(modify: x =>
                x.AddAttachment(x =>
                    x.Name = transmissionAttachmentName));
        });

        // Act
        var response = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Attachments.Should()
            .ContainSingle(attachment =>
                attachment.Name == dialogAttachmentName);
        content.Transmissions.Should().ContainSingle()
            .Which.Attachments.Should().ContainSingle(attachment =>
                attachment.Name == transmissionAttachmentName);
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Get_Dialog_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(ConfigureComplexGetDialogSnapshotData);

        // Act
        var getDialogResult = await Fixture.ServiceownerApi.GetDialog(dialogId, E2EConstants.DefaultParty);

        // Assert
        getDialogResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResult.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }

    private static void ConfigureComplexGetDialogSnapshotData(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
    {
        dialog.SystemLabel = ServiceOwnerSystemLabel.Archive;
        dialog.ServiceOwnerContext = new()
        {
            ServiceOwnerLabels =
            [
                new() { Value = "get-dialog-primary-label" },
                new() { Value = "get-dialog-secondary-label" }
            ]
        };
        dialog.SearchTags =
        [
            new() { Value = "get-dialog-search-primary" },
            new() { Value = "get-dialog-search-secondary" }
        ];
        dialog.Content.MainContentReference = DialogTestData.CreateContentValue(
            value: "https://altinn.no/get-dialog-main-content",
            languageCode: "nb",
            mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown");
        dialog.Attachments =
        [
            new()
            {
                Name = "get-dialog-attachment",
                DisplayName =
                [
                    DialogTestData.CreateLocalization("Get dialog attachment", "en"),
                    DialogTestData.CreateLocalization("Get dialog vedlegg")
                ],
                Urls =
                [
                    new()
                    {
                        Url = new("https://altinn.no/get-dialog-attachment.pdf"),
                        MediaType = "application/pdf",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    },
                    new()
                    {
                        Url = new("https://altinn.no/get-dialog-attachment-api.json"),
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
                value: "https://altinn.no/get-dialog-transmission-content",
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
                        DialogTestData.CreateLocalization("Get dialog transmission navigation")
                    ],
                    Url = new("https://altinn.no/get-dialog-transmission-navigation"),
                    ExpiresAt = new DateTimeOffset(2038, 1, 2, 3, 4, 5, TimeSpan.Zero)
                }
            ];

            if (secondTransmission.Attachments?.FirstOrDefault() is { } transmissionAttachment)
            {
                transmissionAttachment.Urls =
                [
                    new()
                    {
                        Url = new("https://altinn.no/get-dialog-transmission-attachment.pdf"),
                        MediaType = "application/pdf",
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    },
                    new()
                    {
                        Url = new("https://altinn.no/get-dialog-transmission-attachment-api.json"),
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
