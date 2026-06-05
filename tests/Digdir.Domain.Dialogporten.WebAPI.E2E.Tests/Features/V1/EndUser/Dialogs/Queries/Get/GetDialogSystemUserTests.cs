using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogSystemUserTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{

    [E2EFact]
    public async Task Should_Return_200_When_SystemUser_Gets_Dialog_And_Make_Seen_Log()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content;
        content.Should().NotBeNull();
        response.Content!.SeenSinceLastUpdate.ToList()[0].SeenBy.ActorName.Should().Be("Dialogporten E2E tests (nb)");
    }

    [E2EFact]
    public async Task Should_Return_404_When_SystemUser_Gets_Another_Partys_Dialog()
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
