using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;
using FluentAssertions;
using StrawberryShake;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogById;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class AuthorizationTests : GraphQlE2EFixture
{
    [Fact(Explicit = true)]
    public async Task Should_Reject_Request_With_Invalid_EndUser_Token()
    {
        // Arrange
        using var _ = UseEndUserTokenOverrides(tokenOverride: "invalid.jwt.token");
        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("401 (Unauthorized)");
    }

    [Fact(Explicit = true)]
    public async Task Should_Allow_Overriding_EndUser_Ssn_And_Scopes()
    {
        // Arrange
        using var _ = UseEndUserTokenOverrides(
            ssn: "01010112345",
            scopes: "altinn:dialogporten:enduser");

        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetDialog(dialogId);

        // Assert
        result.Errors.Should().NotBeNull();
    }

    private Task<IOperationResult<IGetDialogByIdResult>> GetDialog(Guid dialogId) =>
        GraphQlClient.GetDialogById.ExecuteAsync(dialogId, TestContext.Current.CancellationToken);
}
