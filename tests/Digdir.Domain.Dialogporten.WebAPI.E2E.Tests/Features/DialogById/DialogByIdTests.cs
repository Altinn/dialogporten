using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;
using Digdir.Library.Dialogporten.E2E.Common;
using FluentAssertions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.DialogById;

[Collection(nameof(WebApiTestCollectionFixture))]
public class DialogByIdTests : E2ETestBase<WebApiE2EFixture>
{
    public DialogByIdTests(WebApiE2EFixture fixture) : base(fixture) { }

    [E2EFact]
    public async Task Should_Create_Dialog_Using_ServiceOwner_Api()
    {
        // Act
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Assert
        dialogId.Should().NotBe(Guid.Empty);
    }

    [E2EFact]
    public async Task Should_Get_Dialog_By_Id_Using_ServiceOwner_Api()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: string.Empty,
            TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Id.Should().Be(dialogId);
    }
}
