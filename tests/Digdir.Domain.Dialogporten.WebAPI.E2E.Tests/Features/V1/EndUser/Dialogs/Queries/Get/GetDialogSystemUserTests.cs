using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogSystemUserTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Return_200_And_Make_Seen_Log_When_SystemUser_With_Access_Package_Gets_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.ServiceResource = E2EConstants.DefaultServiceResource;
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        using var _ = Fixture.UseSystemUserTokenOverrides(E2EConstants.DefaultSystemUserId);
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content;
        content.Should().NotBeNull();
        content.SeenSinceLastUpdate.Count.Should().Be(1);
        content.SeenSinceLastUpdate.First().SeenBy.ActorName.Should().Be("Dialogporten E2E tests (nb)");
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Return_200_And_Make_Seen_Log_When_SystemUser_With_Rights_Gets_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.ServiceResource = E2EConstants.AlternateServiceResource;
                d.Party = E2EConstants.AlternateSystemUserOrgUrn;
            }
        );
        using var _ = Fixture.UseSystemUserTokenOverrides(E2EConstants.AlternateSystemUserId);
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content;
        content.Should().NotBeNull();
        content.SeenSinceLastUpdate.Count.Should().Be(1);
        content.SeenSinceLastUpdate.First().SeenBy.ActorName.Should().Be("Dialogporten E2E tests (nb)");
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Return_Forbidden_When_SystemUser_Gets_Another_Partys_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides(E2EConstants.DefaultSystemUserId);
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Return_Forbidden_When_Another_SystemUser_Gets_A_Dialog_Accessible_From_The_Default_SystemUser()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
        {
            d.ServiceResource = E2EConstants.DefaultServiceResource;
            d.Party = E2EConstants.DefaultSystemUserOrgUrn;
        });

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides(E2EConstants.AlternateSystemUserId);
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }
}
