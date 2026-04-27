using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2ETheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Filter_By_ServiceOwnerLabels(bool withEndUserId)
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var controlLabel = Guid.NewGuid().ToString();

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel);
        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(controlLabel);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Party = withEndUserId
                    ? [E2EConstants.DefaultParty]
                    : null!,
                EndUserId = withEndUserId
                    ? E2EConstants.DefaultParty
                    : null!
            }, ct),
            isSuccessful: response =>
                response.Content is { Items.Count: 1 } content
                && content.Items.Single().Id == dialogId
                && content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle();
        content.Items.Single().Id.Should().Be(dialogId);
    }
}
