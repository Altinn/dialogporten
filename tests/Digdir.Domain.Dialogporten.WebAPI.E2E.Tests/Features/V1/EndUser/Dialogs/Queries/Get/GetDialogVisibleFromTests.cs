using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogVisibleFromTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_404_For_NonVisible_Dialog()
    {
        // Arrange
        var visibleFrom = DateTimeOffset.UtcNow.AddMonths(1);
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(modify =>
        {
            modify.VisibleFrom = visibleFrom;
        });

        // Act
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Expires.Should().NotBeNull();
        response.Headers.Expires!.Value.Should().BeCloseToWithinSecond(visibleFrom);

        // Cleanup
        var purgeResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        purgeResponse.IsSuccessful.Should().BeTrue();
    }

    [E2EFact]
    public async Task Should_Return_200_For_Visible_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Id.Should().Be(dialogId);

        // Cleanup
        var purgeResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        purgeResponse.IsSuccessful.Should().BeTrue();
    }
}
