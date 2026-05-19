using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Update;

[Collection(nameof(WebApiTestCollectionFixture))]
public class UpdateDialogSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Put_Dialog_Then_Get_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new() { Value = "put-initial-label" }
                ]
            };
        });

        // Act
        var updateResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsUpdateDialog(
            dialogId,
            CreateFullUpdateDialog(),
            if_Match: null,
            TestContext.Current.CancellationToken);

        var getDialogResult = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        updateResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        getDialogResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResult.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Patch_Dialog_Then_Get_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(dialog =>
        {
            dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new() { Value = "patch-initial-label" }
                ]
            };
        });

        // Act
        var patchResponse = await Fixture.ServiceownerApi.PatchDialogAsync(
            dialogId,
            patchDocument =>
            {
                patchDocument.Add(Replace("/progress", 64));
                patchDocument.Add(Replace("/status", "RequiresAttention"));
                patchDocument.Add(Replace("/externalReference", "patch-updated-external-reference"));
                patchDocument.Add(Replace("/content/title/value/0/value", "Patch updated dialog title"));
                patchDocument.Add(Replace("/searchTags/0/value", "patch-updated-search-tag"));
                patchDocument.Add(Replace("/attachments/0/name", "patch-updated-attachment"));
                patchDocument.Add(Replace("/attachments/0/urls/0/url", "https://example.com/patch-dialog-attachment.pdf"));
                patchDocument.Add(Replace("/guiActions/0/title/0/value", "Patch updated GUI action"));
                patchDocument.Add(Replace("/apiActions/0/endpoints/0/url", "https://example.com/patch-api-action"));
            });

        var getDialogResult = await Fixture.ServiceownerApi.GetDialog(dialogId);

        // Assert
        patchResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        getDialogResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        getDialogResult.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }

    private static JsonPatchOperations_Operation Replace(string path, object value) =>
        new()
        {
            OperationType = JsonPatchOperations_OperationType.Replace,
            Op = "replace",
            Path = path,
            Value = value
        };

    private static V1ServiceOwnerDialogsCommandsUpdate_Dialog CreateFullUpdateDialog() =>
        new()
        {
            Progress = 42,
            ExtendedStatus = "urn:test:update:extended-status",
            ExternalReference = "put-updated-external-reference",
            DueAt = DateTimeOffset.UtcNow.Date.AddYears(2),
            Process = "urn:test:update:process",
            PrecedingProcess = "urn:test:update:preceding-process",
            ExpiresAt = DateTimeOffset.UtcNow.Date.AddYears(20),
            IsApiOnly = false,
            Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.Awaiting,
            Content = new V1ServiceOwnerDialogsCommandsUpdate_Content
            {
                Title = DialogTestData.CreateContentValue(
                    value: "PUT updated dialog title",
                    languageCode: "nb"),
                NonSensitiveTitle = DialogTestData.CreateContentValue(
                    value: "PUT updated non-sensitive title",
                    languageCode: "nb"),
                Summary = DialogTestData.CreateContentValue(
                    value: "PUT updated dialog summary",
                    languageCode: "nb"),
                NonSensitiveSummary = DialogTestData.CreateContentValue(
                    value: "PUT updated non-sensitive summary",
                    languageCode: "nb"),
                SenderName = DialogTestData.CreateContentValue(
                    value: "PUT updated sender",
                    languageCode: "nb"),
                AdditionalInfo = DialogTestData.CreateContentValue(
                    value: "PUT updated additional information",
                    languageCode: "nb",
                    mediaType: "text/markdown"),
                ExtendedStatus = DialogTestData.CreateContentValue(
                    value: "PUT updated status text",
                    languageCode: "nb"),
                MainContentReference = DialogTestData.CreateContentValue(
                    value: "https://example.com/put-main-content",
                    languageCode: "nb",
                    mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown")
            },
            SearchTags =
            [
                new() { Value = "put-search-tag" },
                new() { Value = "put-secondary-search-tag" }
            ],
            Attachments =
            [
                new V1ServiceOwnerDialogsCommandsUpdate_Attachment
                {
                    Id = DialogTestData.NewUuidV7(),
                    Name = "put-dialog-attachment",
                    DisplayName =
                    [
                        DialogTestData.CreateLocalization("PUT dialog attachment")
                    ],
                    Urls =
                    [
                        new V1ServiceOwnerDialogsCommandsUpdate_AttachmentUrl
                        {
                            Id = DialogTestData.NewUuidV7(),
                            Url = new Uri("https://example.com/put-dialog-attachment.pdf"),
                            MediaType = "application/pdf",
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                        },
                        new V1ServiceOwnerDialogsCommandsUpdate_AttachmentUrl
                        {
                            Id = DialogTestData.NewUuidV7(),
                            Url = new Uri("https://example.com/put-dialog-attachment-api.json"),
                            MediaType = "application/json",
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Api
                        }
                    ],
                    ExpiresAt = DateTimeOffset.UtcNow.Date.AddYears(10)
                }
            ],
            Transmissions =
            [
                new V1ServiceOwnerDialogsCommandsUpdate_Transmission
                {
                    Id = DialogTestData.NewUuidV7(),
                    IdempotentKey = "put-transmission-key",
                    CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    AuthorizationAttribute = E2EConstants.AvailableExternalResource,
                    ExtendedType = new Uri("urn:test:update:transmission-type"),
                    ExternalReference = "put-transmission-external-reference",
                    Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                    Sender = new V1ServiceOwnerCommonActors_Actor
                    {
                        ActorType = Actors_ActorType.ServiceOwner
                    },
                    Content = new V1ServiceOwnerDialogsCommandsUpdate_TransmissionContent
                    {
                        Title = DialogTestData.CreateContentValue(
                            value: "PUT transmission title",
                            languageCode: "nb"),
                        Summary = DialogTestData.CreateContentValue(
                            value: "PUT transmission summary",
                            languageCode: "nb"),
                        ContentReference = DialogTestData.CreateContentValue(
                            value: "https://example.com/put-transmission-content",
                            languageCode: "nb",
                            mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown")
                    },
                    Attachments =
                    [
                        new V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachment
                        {
                            Id = DialogTestData.NewUuidV7(),
                            Name = "put-transmission-attachment",
                            DisplayName =
                            [
                                DialogTestData.CreateLocalization("PUT transmission attachment")
                            ],
                            Urls =
                            [
                                new V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachmentUrl
                                {
                                    Url = new Uri("https://example.com/put-transmission-attachment.pdf"),
                                    MediaType = "application/pdf",
                                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                                }
                            ],
                            ExpiresAt = DateTimeOffset.UtcNow.Date.AddYears(10)
                        }
                    ],
                    NavigationalActions =
                    [
                        new V1ServiceOwnerDialogsCommandsUpdate_TransmissionNavigationalAction
                        {
                            Title =
                            [
                                DialogTestData.CreateLocalization("PUT transmission navigation")
                            ],
                            Url = new Uri("https://example.com/put-transmission-navigation"),
                            ExpiresAt = DateTimeOffset.UtcNow.Date.AddYears(10)
                        }
                    ]
                }
            ],
            GuiActions =
            [
                new V1ServiceOwnerDialogsCommandsUpdate_GuiAction
                {
                    Id = DialogTestData.NewUuidV7(),
                    Action = "read",
                    Url = new Uri("https://example.com/put-gui-action"),
                    AuthorizationAttribute = E2EConstants.AvailableExternalResource,
                    IsDeleteDialogAction = false,
                    HttpMethod = Http_HttpVerb.POST,
                    Priority = DialogsEntitiesActions_DialogGuiActionPriority.Primary,
                    Title =
                    [
                        DialogTestData.CreateLocalization("PUT GUI action")
                    ],
                    Prompt =
                    [
                        DialogTestData.CreateLocalization("PUT GUI action prompt")
                    ]
                }
            ],
            ApiActions =
            [
                new V1ServiceOwnerDialogsCommandsUpdate_ApiAction
                {
                    Id = DialogTestData.NewUuidV7(),
                    Action = "read",
                    AuthorizationAttribute = E2EConstants.AvailableExternalResource,
                    Name = "put-api-action",
                    Endpoints =
                    [
                        new V1ServiceOwnerDialogsCommandsUpdate_ApiActionEndpoint
                        {
                            Id = DialogTestData.NewUuidV7(),
                            Version = "v1",
                            Url = new Uri("https://example.com/put-api-action"),
                            HttpMethod = Http_HttpVerb.POST,
                            DocumentationUrl = new Uri("https://example.com/put-api-action/docs"),
                            RequestSchema = new Uri("https://example.com/put-api-action/request-schema.json"),
                            ResponseSchema = new Uri("https://example.com/put-api-action/response-schema.json"),
                            Deprecated = true,
                            SunsetAt = DateTimeOffset.UtcNow.Date.AddYears(10)
                        }
                    ]
                }
            ],
            Activities =
            [
                new V1ServiceOwnerDialogsCommandsUpdate_Activity
                {
                    Id = DialogTestData.NewUuidV7(),
                    CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    ExtendedType = new Uri("urn:test:update:activity-type"),
                    Type = DialogsEntitiesActivities_DialogActivityType.Information,
                    PerformedBy = new V1ServiceOwnerCommonActors_Actor
                    {
                        ActorType = Actors_ActorType.PartyRepresentative,
                        ActorName = "PUT activity performer"
                    },
                    Description =
                    [
                        DialogTestData.CreateLocalization("PUT activity description")
                    ]
                }
            ]
        };
}
