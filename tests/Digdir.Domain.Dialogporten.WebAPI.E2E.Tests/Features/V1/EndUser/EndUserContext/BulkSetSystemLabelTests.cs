using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.EndUser.Features.V1.SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class BulkSetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_BulkSet_Labels_For_Accessible_Dialogs()
    {
        // Arrange
        var createDialog1 = Fixture.ServiceownerApi.CreateComplexDialogAsync();
        var createDialog2 = Fixture.ServiceownerApi.CreateComplexDialogAsync();
        await Task.WhenAll(createDialog1, createDialog2);

        // Act
        var response = await Fixture.EndUserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = createDialog1.Result },
                new() { DialogId = createDialog2.Result }
            ];

            request.AddLabels = [Bin];
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var dialog1Response = await Fixture.EndUserApi.GetDialog(createDialog1.Result);
        var dialog2Response = await Fixture.EndUserApi.GetDialog(createDialog2.Result);

        dialog1Response.ShouldHaveStatusCode(HttpStatusCode.OK);
        dialog2Response.ShouldHaveStatusCode(HttpStatusCode.OK);

        dialog1Response.Content!.EndUserContext.SystemLabels
            .Should().ContainSingle()
            .Which.Should().Be(Bin);

        dialog2Response.Content!.EndUserContext.SystemLabels
            .Should().ContainSingle()
            .Which.Should().Be(Bin);
    }

    [E2EFact]
    public async Task Should_Be_Able_To_Set_System_Labels_When_SystemUser()
    {
        // Arrange
        var createDialog1 = Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        var createDialog2 = Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        await Task.WhenAll(createDialog1, createDialog2);

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var response = await Fixture.EndUserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = createDialog1.Result },
                new() { DialogId = createDialog2.Result },
            ];
            request.AddLabels = [Archive];
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
    }

    [E2EFact]
    public async Task Should_Return_404_When_BulkSet_Contains_Unauthorized_Dialog()
    {
        // Arrange
        var createDialog1 = Fixture.ServiceownerApi.CreateComplexDialogAsync();
        var createDialog2 = Fixture.ServiceownerApi.CreateComplexDialogAsync();
        await Task.WhenAll(createDialog1, createDialog2);

        var forbiddenDialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(dialog => dialog.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}" +
                                                              $"{E2EConstants.GetDefaultServiceOwnerOrgNr()}");

        // Act
        var response = await Fixture.EndUserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = createDialog1.Result },
                new() { DialogId = createDialog2.Result },
                new() { DialogId = forbiddenDialogId }
            ];
            request.AddLabels = [Archive];
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().Contain(forbiddenDialogId.ToString());
    }

    [E2EFact]
    public async Task Should_Return_404_As_SystemUser_When_BulkSet_Contains_Unauthorized_Dialog()
    {
        // Arrange
        var createDialog1 = Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        var createDialog2 = Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
            {
                d.Party = E2EConstants.DefaultSystemUserOrgUrn;
            }
        );
        await Task.WhenAll(createDialog1, createDialog2);

        var forbiddenDialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(d =>
        {
            d.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}" +
                      $"{E2EConstants.GetDefaultServiceOwnerOrgNr()}";
        });

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var response = await Fixture.EndUserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = createDialog1.Result },
                new() { DialogId = createDialog2.Result },
                new() { DialogId = forbiddenDialogId }
            ];
            request.AddLabels = [Archive];
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().Contain(forbiddenDialogId.ToString());
    }
}
