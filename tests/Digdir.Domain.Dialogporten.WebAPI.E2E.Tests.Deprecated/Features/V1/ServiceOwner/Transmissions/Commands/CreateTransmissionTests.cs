using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Deprecated.Features.V1.ServiceOwner.Transmissions.Commands;

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
}
