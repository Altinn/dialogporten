using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;
using FluentAssertions;
using StrawberryShake;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogById;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class DialogByIdTests : GraphQlE2EFixture
{
    [Fact(Explicit = true)]
    public async Task Should_Return_Typed_NotFound_Error_For_Invalid_DialogId()
    {
        // Arrange
        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Data.Should().NotBeNull();

        var error = result.Data.DialogById.Errors.Single();

        error.Should().BeOfType<GetDialogById_DialogById_Errors_DialogByIdNotFound>();
        error.Message.Should().Contain(dialogId.ToString());
    }

    [Fact(Explicit = true)]
    public async Task Should_Return_Dialog_For_Valid_DialogId()
    {
        // Arrange
        var dialogId = await CreateSimpleDialog();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Data.Should().NotBeNull();

        var dialog = result.Data.DialogById.Dialog;
        dialog.Should().NotBeNull();
        dialog.Id.Should().Be(dialogId);
    }

    private Task<IOperationResult<IGetDialogByIdResult>> GetDialog(Guid dialogId) =>
        GraphQlClient.GetDialogById.ExecuteAsync(dialogId, TestContext.Current.CancellationToken);

    private async Task<Guid> CreateSimpleDialog()
    {
        var createDialogResponse =
            await ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                DialogTestData.CreateDialog(
                    serviceResource: "urn:altinn:resource:ttd-dialogporten-automated-tests",
                    party: $"urn:altinn:person:identifier-no:{TestTokenConstants.DefaultEndUserSsn}",
                    content: DialogTestData.CreateContent(
                        title: DialogTestData.CreateContentValue(
                            value: "Skjema for rapportering av et eller annet",
                            languageCode: "nb"),
                        summary: DialogTestData.CreateContentValue(
                            value: "Et sammendrag her. Maks 200 tegn, ingen HTML-støtte. Påkrevd. Vises i liste.",
                            languageCode: "nb"),
                        senderName: DialogTestData.CreateContentValue(
                            value: "Avsendernavn",
                            languageCode: "nb"),
                        additionalInfo: DialogTestData.CreateContentValue(
                            value: "Utvidet forklaring (enkel HTML-støtte, inntil 1023 tegn). Ikke påkrevd. Vises kun i detaljvisning.",
                            languageCode: "nb",
                            mediaType: "text/plain"),
                        extendedStatus: DialogTestData.CreateContentValue(
                            value: "Utvidet status",
                            languageCode: "nb",
                            mediaType: "text/plain"))),
                TestContext.Current.CancellationToken);

        createDialogResponse.Content.Should().NotBeNull();

        var dialogId = createDialogResponse.Content
            .Replace("\"", string.Empty);

        return Guid.Parse(dialogId);
    }
}
