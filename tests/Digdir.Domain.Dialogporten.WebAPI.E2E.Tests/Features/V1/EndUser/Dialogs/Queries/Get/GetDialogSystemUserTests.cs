using System.Net;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogSystemUserTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{

    [E2EFact]
    public async Task Should_Return_404_When_SystemUser_Gets_Dialog_Directly()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }
}
