using System.Globalization;
using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
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
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(
            dialog => dialog.VisibleFrom = visibleFrom);

        // Act
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var hasExpiresHeader = response.ContentHeaders!.TryGetValues("Expires", out var expiresValues);
        hasExpiresHeader.Should().BeTrue("Expires header should be present");

        var expires = DateTimeOffset.Parse(expiresValues!.First(), CultureInfo.InvariantCulture);
        expires.BeCloseToWithinSecond(visibleFrom);
    }
}
