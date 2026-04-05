using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class BulkSetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_BulkSet_Labels_For_Accessible_Dialogs()
    {
        // Arrange
        var dialogId1 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogId2 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.EnduserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = dialogId1 },
                new() { DialogId = dialogId2 }
            ];

            request.AddLabels = [Bin];
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var dialog1Response = await Fixture.EnduserApi.GetDialog(dialogId1);
        var dialog2Response = await Fixture.EnduserApi.GetDialog(dialogId2);

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
    public async Task Should_Return_404_When_BulkSet_Contains_Unauthorized_Dialog()
    {
        // Arrange
        var dialogId1 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogId2 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var forbiddenDialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(dialog =>
                dialog.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}" +
                               $"{E2EConstants.GetDefaultServiceOwnerOrgNr()}");

        // Act
        var response = await Fixture.EnduserApi.BulkSetSystemLabels(request =>
        {
            request.Dialogs =
            [
                new() { DialogId = dialogId1 },
                new() { DialogId = dialogId2 },
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
