using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class ServiceownerApiExtensions
{
    public static async Task<Guid> CreateSimpleDialogAsync(
        this IServiceownerApi serviceownerApi)
    {
        ArgumentNullException.ThrowIfNull(serviceownerApi);

        var createDialogResponse =
            await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
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

        var dialogIdRaw = createDialogResponse
            .Content
            .Trim('"');

        if (!Guid.TryParse(dialogIdRaw, out var dialogId))
        {
            Assert.Fail($"Could not parse create dialog response, {dialogIdRaw}");
        }

        dialogId.Should().NotBe(Guid.Empty);

        return dialogId;
    }
}
