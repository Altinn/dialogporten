using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateTransmissionTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Transmission_Attachment_Name()
    {
        // Arrange
        const string transmissionAttachmentName = "transmission-attachment";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var transmission = DialogTestData.CreateSimpleTransmission(x =>
            x.AddAttachment(x => x.Name = transmissionAttachmentName));

        // Act
        var createResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(
                dialogId,
                transmission,
                if_Match: null,
                TestContext.Current.CancellationToken);

        createResponse.ShouldHaveStatusCode(HttpStatusCode.Created);
        var transmissionId = createResponse.Content.ToGuid();

        var response =
            await Fixture.ServiceownerApi
                .V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(
                    dialogId, transmissionId, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Attachments.Should().ContainSingle().Which.Name.Should().Be(transmissionAttachmentName);
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Create_Transmissions_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var parentTransmissionId = Guid.CreateVersion7();

        // First (parent) transmission: service owner sender, title + summary + content reference,
        // an attachment with URLs, and a navigational action.
        var firstTransmission = new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest
        {
            Id = parentTransmissionId,
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            ExternalReference = "first-transmission",
            Sender = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.ServiceOwner
            },
            Content = new()
            {
                Title = DialogTestData.CreateContentValue(
                    value: "Forsendelsestittel",
                    languageCode: "nb"),
                Summary = DialogTestData.CreateContentValue(
                    value: "Forsendelse oppsummering",
                    languageCode: "nb"),
                ContentReference = DialogTestData.CreateContentValue(
                    mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown",
                    value: [DialogTestData.CreateLocalization("https://digdir.com/content-reference")])
            },
            Attachments =
            [
                new()
                {
                    Name = "first-attachment-name",
                    DisplayName = [DialogTestData.CreateLocalization("First attachment display name")],
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                    Urls =
                    [
                        new()
                        {
                            Url = new Uri("https://digdir.com/first-attachment-url"),
                            MediaType = "application/pdf",
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                        }
                    ]
                }
            ],
            NavigationalActions =
            [
                new()
                {
                    Title = [DialogTestData.CreateLocalization("First action title")],
                    Url = new Uri("https://digdir.com/first-action-url")
                }
            ]
        };

        // Second transmission: party representative sender, related to the first transmission,
        // its own attachment and navigational action.
        var secondTransmission = new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest
        {
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Submission,
            ExternalReference = "second-transmission",
            ExtendedType = new Uri("https://digdir.com/transmission-type"),
            RelatedTransmissionId = parentTransmissionId,
            Sender = new()
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorId = $"urn:altinn:organization:identifier-no:{E2EConstants.GetDefaultServiceOwnerOrgNr()}"
            },
            Content = new()
            {
                Title = DialogTestData.CreateContentValue(
                    value: "Melding med vedlegg",
                    languageCode: "nb")
            },
            Attachments =
            [
                new()
                {
                    Name = "second-attachment-name",
                    DisplayName = [DialogTestData.CreateLocalization("Second attachment display name")],
                    Urls =
                    [
                        new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachmentUrl
                        {
                            Url = new Uri("https://digdir.com/second-attachment-url"),
                            MediaType = "application/pdf",
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                        }
                    ]
                }
            ],
            NavigationalActions =
            [
                new()
                {
                    Title = [DialogTestData.CreateLocalization("Second action title")],
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                    Url = new Uri("https://digdir.com/second-action-url")
                }
            ]
        };

        // Act
        var firstCreateResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(
                dialogId,
                firstTransmission,
                if_Match: null,
                TestContext.Current.CancellationToken);
        firstCreateResponse.ShouldHaveStatusCode(HttpStatusCode.Created);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(firstCreateResponse.Content),
            fileNameSuffix: "created-response");

        var secondCreateResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(
                dialogId,
                secondTransmission,
                if_Match: null,
                TestContext.Current.CancellationToken);
        secondCreateResponse.ShouldHaveStatusCode(HttpStatusCode.Created);

        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsQueriesSearchTransmissionsDialogTransmission(
                dialogId,
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
