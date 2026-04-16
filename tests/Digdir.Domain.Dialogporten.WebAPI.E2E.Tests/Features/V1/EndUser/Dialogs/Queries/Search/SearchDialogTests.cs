using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Support_Pagination_With_Limit_And_ContinuationToken()
    {
        // Arrange - create 4 dialogs with a unique sentinel tag
        var sentinelTag = Guid.NewGuid().ToString();
        var createdIds = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var id = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            {
                x.SearchTags = [new() { Value = sentinelTag }];
            });
            createdIds.Add(id);
        }

        // Act - first page with limit 2
        var firstPage = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelTag,
                Limit = 2
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Count == 2,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert first page
        firstPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        firstPage.Content!.Items.Should().HaveCount(2);
        firstPage.Content.HasNextPage.Should().BeTrue();
        firstPage.Content.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        // Act - second page using continuation token
        var secondPage = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
        {
            Party = [E2EConstants.DefaultParty],
            Search = sentinelTag,
            Limit = 2,
            ContinuationToken = firstPage.Content.ContinuationToken
        }, new());

        // Assert second page has different IDs
        secondPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        secondPage.Content!.Items.Should().HaveCount(2);

        var firstPageIds = firstPage.Content.Items!.Select(x => x.Id).ToList();
        var secondPageIds = secondPage.Content.Items!.Select(x => x.Id).ToList();
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [E2EFact]
    public async Task Should_Not_See_Sensitive_Content_With_Inadequate_Auth_Level()
    {
        // Arrange
        const string sensitiveTitle = "Sensitive title!";
        const string nonSensitiveTitle = "Non-sensitive title!";

        const string sensitiveSummary = "Sensitive summary!";
        const string nonSensitiveSummary = "Non-sensitive summary!";

        var sentinelLabel = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
        {
            // This serviceResource requires auth level 4
            x.ServiceResource = "urn:altinn:resource:ttd-dialogporten-transmissions-test";

            x.Content.Title = GetContentValue(sensitiveTitle);
            x.Content.Summary = GetContentValue(sensitiveSummary);

            x.Content.NonSensitiveTitle = GetContentValue(nonSensitiveTitle);
            x.Content.NonSensitiveSummary = GetContentValue(nonSensitiveSummary);

            x.SearchTags = [new() { Value = sentinelLabel }];
        });

        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelLabel
            }, new(), ct),
            isSuccessful: searchResult => searchResult.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        searchResult.Content!.Items.Should().NotBeNull();
        searchResult.Content.Items.Should().HaveCount(1);
        searchResult.Content!.Items.Should()
            .ContainSingle(x => x.Id == dialogId);

        var dialog = searchResult.Content!.Items.Single();
        dialog.Content.Title.Value.First().Value.Should().NotBe(sensitiveTitle);
        dialog.Content.Title.Value.First().Value.Should().Be(nonSensitiveTitle);

        dialog.Content.Summary.Value.First().Value.Should().NotBe(sensitiveSummary);
        dialog.Content.Summary.Value.First().Value.Should().Be(nonSensitiveSummary);
    }

    private static Altinn.ApiClients.Dialogporten.Features.V1.V1CommonContent_ContentValue GetContentValue(string value) => new()
    {
        Value = [new() { Value = value, LanguageCode = "en" }]
    };
}
