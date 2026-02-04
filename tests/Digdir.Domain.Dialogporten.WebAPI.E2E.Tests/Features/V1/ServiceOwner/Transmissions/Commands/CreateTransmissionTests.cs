using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Xunit;

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

        var createResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(
                dialogId,
                transmission,
                if_Match: null,
                TestContext.Current.CancellationToken);

        createResponse.IsSuccessful.Should().BeTrue();

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Transmissions.Should().Contain(t =>
            t.Attachments.Any(a => a.Name == transmissionAttachmentName));
    }
}
