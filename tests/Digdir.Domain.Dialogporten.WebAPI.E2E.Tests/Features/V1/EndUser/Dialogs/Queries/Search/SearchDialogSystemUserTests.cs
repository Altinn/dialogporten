using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogSystemUserTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_404_When_SystemUser_Gets_Dialog_Directly()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_Empty_Items_When_SystemUser_Searches_With_Party()
    {
        // Arrange
        var __ = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var queryParams = new V1EndUserDialogsQueriesSearchDialogQueryParams
        {
            Search = "system-title",
            Party = [E2EConstants.DefaultParty]
        };

        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(
            queryParams,
            accept_Language: new());

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content;
        content.Should().NotBeNull();
        content.Items.Should().BeNull();
    }
}
