using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;
using FluentAssertions;
using StrawberryShake;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogById;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class DialogByIdTests : IGraphQlE2ETestHooks
{
    private readonly GraphQlE2EFixture _fixture;
    private const bool ExplicitTests = false;

    public DialogByIdTests(GraphQlE2EFixture fixture)
    {
        _fixture = fixture;
    }

    void IGraphQlE2ETestHooks.BeforeTest() => _fixture.PreflightCheck();

    void IGraphQlE2ETestHooks.AfterTest() => _fixture.CleanupAfterTest();

    [GraphQlE2EFact(Explicit = ExplicitTests)]
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

    [GraphQlE2EFact(Explicit = ExplicitTests)]
    // [Fact]
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

    [GraphQlE2EFact(Explicit = ExplicitTests)]
    public async Task Should_Return_401_Unauthorized_With_Invalid_EndUser_Token()
    {
        // Arrange
        using var _ = _fixture.UseEndUserTokenOverrides(tokenOverride: "invalid.jwt.token");
        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("401 (Unauthorized)");
    }

    [GraphQlE2EFact(Explicit = ExplicitTests)]
    public async Task Should_Return_Typed_NotFound_Result_When_Using_Unauthorized_Party()
    {
        // Arrange
        var dialogId = await CreateSimpleDialog();
        var foo = await CreateSimpleDialog();
        var bar = await CreateSimpleDialog();

        var searchResult = await _fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            ServiceResource = ["urn:altinn:resource:ttd-dialogporten-automated-tests"],
        }, TestContext.Current.CancellationToken);
        Console.WriteLine(searchResult.IsSuccessful);
        Console.WriteLine(foo);
        Console.WriteLine(bar);

        // Act
        // Fetching dialog with default EndUser, should return dialog
        var authorizedResult = await GetDialog(dialogId);

        using var _ = _fixture.UseEndUserTokenOverrides(ssn: "27069815400");
        var unauthorizedResult = await GetDialog(dialogId);

        // Assert
        authorizedResult.Data.Should().NotBeNull();
        authorizedResult.Data.DialogById.Dialog!.Id.Should().Be(dialogId);

        unauthorizedResult.Data.Should().NotBeNull();
        var error = unauthorizedResult.Data.DialogById.Errors.Single();

        error.Should().BeOfType<GetDialogById_DialogById_Errors_DialogByIdNotFound>();
        error.Message.Should().Contain(dialogId.ToString());
    }

    private Task<IOperationResult<IGetDialogByIdResult>> GetDialog(Guid dialogId) =>
        _fixture.GraphQlClient.GetDialogById.ExecuteAsync(dialogId, TestContext.Current.CancellationToken);

    private async Task<Guid> CreateSimpleDialog()
    {
        var createDialogResponse =
            await _fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
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
