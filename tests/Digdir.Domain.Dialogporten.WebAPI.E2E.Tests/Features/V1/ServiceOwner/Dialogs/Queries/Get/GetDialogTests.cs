using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
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
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Attachments.Should()
            .ContainSingle(attachment =>
                attachment.Name == dialogAttachmentName);
        content.Transmissions.Should().ContainSingle()
            .Which.Attachments.Should().ContainSingle(attachment =>
                attachment.Name == transmissionAttachmentName);
    }
}
