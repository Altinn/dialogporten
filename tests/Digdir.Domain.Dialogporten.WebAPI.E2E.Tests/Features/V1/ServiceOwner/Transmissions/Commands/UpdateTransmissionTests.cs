using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(WebApiTestCollectionFixture))]
public class UpdateTransmissionTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private static string ChangeTransmissionScopes =>
        E2EConstants.ServiceOwnerScopes + " " +
        AuthorizationScope.ServiceProviderChangeTransmissions;

    [E2EFact]
    public async Task Should_Update_Transmission_And_Persist_Attachments_And_NavigationalActions()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: ChangeTransmissionScopes);
        const string updatedExternalReference = "updated-external-reference-e2e";
        const string updatedAttachmentName = "updated-attachment";
        var updatedNavigationalActionUrl = new Uri("https://example.com/updated-action");

        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, _) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(x => x.Id = transmissionId));

        var request = CreateUpdateRequest(transmissionId, x =>
        {
            x.ExternalReference = updatedExternalReference;
            x.Attachments =
            [
                new V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionAttachment
                {
                    Name = updatedAttachmentName,
                    DisplayName = [DialogTestData.CreateLocalization("Updated attachment")],
                    Urls =
                    [
                        new V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionAttachmentUrl
                        {
                            Url = new Uri("https://example.com/updated-attachment.pdf"),
                            MediaType = "application/pdf",
                            ConsumerType = Altinn.ApiClients.Dialogporten.Features.V1.Attachments_AttachmentUrlConsumerType.Gui
                        }
                    ]
                }
            ];
            x.NavigationalActions =
            [
                new V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionNavigationalAction
                {
                    Title = [DialogTestData.CreateLocalization("Updated action")],
                    Url = updatedNavigationalActionUrl
                }
            ];
        });

        // Act
        var updateResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: null,
                TestContext.Current.CancellationToken);

        var getResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(
                dialogId,
                transmissionId,
                TestContext.Current.CancellationToken);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.IsSuccessful.Should().BeTrue();

        var transmission = getResponse.Content ?? throw new InvalidOperationException("Transmission content was null.");
        transmission.ExternalReference.Should().Be(updatedExternalReference);
        transmission.Attachments.Should().ContainSingle().Which.Name.Should().Be(updatedAttachmentName);
        transmission.NavigationalActions.Should().ContainSingle().Which.Url.Should().Be(updatedNavigationalActionUrl);
    }

    [E2EFact]
    public async Task Should_Return_PreconditionFailed_When_IfMatch_DialogRevision_Is_Changed()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: ChangeTransmissionScopes);
        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, _) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(x => x.Id = transmissionId));
        var request = CreateUpdateRequest(transmissionId, x => x.ExternalReference = "if-match-mismatch");

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: Guid.CreateVersion7(),
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [E2EFact]
    public async Task Should_Return_Forbidden_Without_ChangeTransmission_Scope()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, _) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(x => x.Id = transmissionId));

        var request = CreateUpdateRequest(transmissionId, x => x.ExternalReference = "forbidden-update");

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [E2EFact]
    public async Task Should_Return_BadRequest_When_ContentReference_Is_Not_Https()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: ChangeTransmissionScopes);
        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, _) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(x => x.Id = transmissionId));

        var request = CreateUpdateRequest(transmissionId, x =>
            x.Content.ContentReference = DialogTestData.CreateContentValue(
                value: "http://example.com/not-https",
                languageCode: "nb"));

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Return_Conflict_When_IdempotentKey_Is_Already_Used()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: ChangeTransmissionScopes);
        const string idempotentKey = "duplicate-idempotent-key";

        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, _) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.AddTransmission(x => x.IdempotentKey = idempotentKey);
            x.AddTransmission(x => x.Id = transmissionId);
        });
        var request = CreateUpdateRequest(transmissionId, x => x.IdempotentKey = idempotentKey);

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: null,
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [E2EFact]
    public async Task Should_Return_New_Dialog_Revision_After_Updating_Transmission()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: ChangeTransmissionScopes);
        var transmissionId = DialogTestData.NewUuidV7();
        var (dialogId, revisionBeforeUpdate) = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            x.AddTransmission(x => x.Id = transmissionId));
        var request = CreateUpdateRequest(transmissionId, x => x.ExternalReference = "revision-change");

        // Act
        var updateResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(
                dialogId,
                transmissionId,
                request,
                if_Match: null,
                TestContext.Current.CancellationToken);

        var revisionAfterUpdate = updateResponse.Headers.ETagToGuid();

        var afterUpdateDialog = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        revisionAfterUpdate.Should().NotBe(revisionBeforeUpdate);
        afterUpdateDialog.Content.Should().NotBeNull();
        afterUpdateDialog.Content!.Revision.Should().Be(revisionAfterUpdate);
    }

    private static V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest CreateUpdateRequest(
        Guid transmissionId,
        Action<V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest>? modify = null)
    {
        var request = new V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest
        {
            Id = transmissionId,
            IsSilentUpdate = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Type = Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            Sender = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType.ServiceOwner
            },
            Content = new V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionContent
            {
                Title = DialogTestData.CreateContentValue(
                    value: "Updated transmission",
                    languageCode: "nb")
            },
            Attachments = [],
            NavigationalActions = []
        };

        modify?.Invoke(request);
        return request;
    }
}
