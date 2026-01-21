using Digdir.Library.Dialogporten.E2E.Common;
using FluentAssertions;
using StrawberryShake;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogById;

[Collection(nameof(TestCollectionFixture))]
public class DialogByIdTests : E2ETestBase
{

    public DialogByIdTests(E2EFixture fixture) : base(fixture) { }

    [E2EFact]
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

    [E2EFact]
    public async Task Should_Return_Dialog_For_Valid_DialogId()
    {
        // Arrange
        var dialogId = await DialogE2EHelpers.CreateSimpleDialogAsync(Fixture);

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Data.Should().NotBeNull();

        var dialog = result.Data.DialogById.Dialog;
        dialog.Should().NotBeNull();
        dialog.Id.Should().Be(dialogId);
    }

    [E2EFact]
    public async Task Should_Return_401_Unauthorized_With_Invalid_EndUser_Token()
    {
        // Arrange
        using var _ = Fixture.UseEndUserTokenOverrides(tokenOverride: "invalid.jwt.token");
        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("401 (Unauthorized)");
    }

    [E2EFact]
    public async Task Should_Return_Typed_NotFound_Result_When_Using_Unauthorized_Party()
    {
        // Arrange
        var dialogId = await DialogE2EHelpers.CreateSimpleDialogAsync(Fixture);

        // Act
        // Fetching dialog with default EndUser, should return dialog
        var authorizedResult = await GetDialog(dialogId);

        using var _ = Fixture.UseEndUserTokenOverrides(ssn: "27069815400");
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
        Fixture.GraphQlClient.GetDialogById.ExecuteAsync(dialogId, TestContext.Current.CancellationToken);
}
