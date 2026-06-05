using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogSystemUserTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Allowed_Dialogs_When_SystemUser_Searches()
    {
        // Arrange
        var uniqueExtendedStatus = Guid.NewGuid().ToString();
        var __ = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
        {
            d.ExtendedStatus = uniqueExtendedStatus;
            d.Party = E2EConstants.DefaultSystemUserOrgUrn;
        });

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var queryParams = new SearchDialogsQueryParams
        {
            ExtendedStatus = [uniqueExtendedStatus],
            Party = [E2EConstants.DefaultSystemUserOrgUrn]
        };

        var response = await Fixture.EndUserApi.V1.SearchDialogs(
            queryParams,
            accept_Language: new());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content;
        content.Should().NotBeNull();
        content.Items.Should().NotBeNull();
        content.Items.Count.Should().Be(1);
    }

    [E2EFact]
    public async Task Should_Return_Empty_Items_When_SystemUser_Searches_Other_Parties()
    {
        // Arrange
        var uniqueExtendedStatus = Guid.NewGuid().ToString();
        var __ = await Fixture.ServiceownerApi.CreateComplexDialogAsync(d =>
        {
            d.ExtendedStatus = uniqueExtendedStatus;
            d.Party = "urn:altinn:organization:identifier-no:437454302";
        });

        // Act
        using var _ = Fixture.UseSystemUserTokenOverrides();
        var queryParams = new SearchDialogsQueryParams
        {
            ExtendedStatus = [uniqueExtendedStatus],
            Party = ["urn:altinn:organization:identifier-no:437454302"]
        };

        var response = await Fixture.EndUserApi.V1.SearchDialogs(
            queryParams,
            accept_Language: new());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content;
        content.Should().NotBeNull();
        content.Items.Should().BeNull();
    }
}
