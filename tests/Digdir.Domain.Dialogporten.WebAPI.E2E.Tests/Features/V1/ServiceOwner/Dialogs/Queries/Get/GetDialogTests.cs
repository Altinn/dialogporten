using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Xunit;

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
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Id.Should().Be(dialogId);
    }

    [E2EFact]
    public async Task Should_Return_Attachment_Names()
    {
        // Arrange
        var dialog = DialogTestData.GetSimpleCreateDialogCommand();

        dialog.Attachments =
        [
            DialogTestData.CreateDialogAttachment(name: "dialog-attachment")
        ];

        dialog.Transmissions =
        [
            DialogTestData.CreateTransmission(
                title: "Melding med vedlegg",
                attachments:
                [
                    DialogTestData.CreateTransmissionAttachment(
                        name: "transmission-attachment",
                        url: "https://example.com/transmission-attachment.pdf")
                ])
        ];

        var createResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        createResponse.IsSuccessful.Should().BeTrue();
        var dialogIdRaw = createResponse.Content?.Trim('"')
            ?? throw new InvalidOperationException("Dialog id was null.");

        if (!Guid.TryParse(dialogIdRaw, out var dialogId))
        {
            throw new InvalidOperationException($"Could not parse create dialog response, {dialogIdRaw}");
        }

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Attachments.Should().ContainSingle(attachment => attachment.Name == "dialog-attachment");
        content.Transmissions.Should().ContainSingle()
            .Which.Attachments.Should().ContainSingle(attachment => attachment.Name == "transmission-attachment");
    }
}
