using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.Attachments_AttachmentUrlConsumerType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Transmissions.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchTransmissionTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Search_Transmissions()
    {
        // Arrange
        var transmissionId = Guid.CreateVersion7();

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.AddTransmission(transmission => transmission.Id = transmissionId));

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchTransmissionsDialogTransmission(
            dialogId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Transmission content was null.");
        content.Should().ContainSingle().Which.Id.Should().Be(transmissionId);
    }

    [E2EFact]
    public async Task Search_Transmissions_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
        {
            var transmissionId = Guid.CreateVersion7();
            x.Transmissions.Clear();
            x.AddTransmission(x => x.Id = transmissionId);
            x.AddTransmission(x =>
            {
                x.Sender = new()
                {
                    ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType.PartyRepresentative,
                    ActorId = $"urn:altinn:organization:identifier-no:{E2EConstants.GetDefaultServiceOwnerOrgNr()}"
                };
                x.Content.Summary = new() { Value = [DialogTestData.CreateLocalization("Summary")] };
                x.Content.ContentReference = new()
                {
                    MediaType = "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown",
                    Value = [DialogTestData.CreateLocalization("https://digdir.com/content-reference")]
                };
                x.RelatedTransmissionId = transmissionId;
                x.ExtendedType = new Uri("https://digdir.com/transmission-type");
                x.IdempotentKey = "idempotent-key";
                x.ExternalReference = "external-reference";
                x.NavigationalActions =
                [
                    new()
                    {
                        ExpiresAt = DateTime.UtcNow.AddYears(100),
                        Title = [DialogTestData.CreateLocalization("Action title")],
                        Url = new Uri("https://digdir.com/action-url")
                    }
                ];
                x.Attachments =
                [
                    new()
                    {
                        Name = "attachment-name",
                        ExpiresAt = DateTime.UtcNow.AddYears(100),
                        DisplayName = [DialogTestData.CreateLocalization("Attachment display name")],
                        Urls =
                        [
                            new()
                            {
                                Url = new Uri("https://digdir.com/attachment-url"),
                                MediaType = "application/pdf",
                                ConsumerType = Gui
                            }
                        ]
                    }
                ];
            });
        });

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchTransmissionsDialogTransmission(
            dialogId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content),
            fileNameSuffix: Fixture.DotnetEnvironment);
    }
}
