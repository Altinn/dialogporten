using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private static readonly HttpClient HttpClient = new();

    [E2EFact]
    public async Task Should_Return_SeenSinceLastUpdate_When_Dialog_Has_Been_Viewed()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger seen log by getting the dialog
        var getResponse = await Fixture.EnduserApi.GetDialog(dialogId);
        getResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty]
            }, new(), ct),
            isSuccessful: r => r.Content?.Items
                .FirstOrDefault(x => x.Id == dialogId)
                ?.SeenSinceLastUpdate.Count > 0,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var dialog = searchResult.Content.Items.Single(x => x.Id == dialogId);
        dialog.SeenSinceLastUpdate.Should().HaveCount(1);
        dialog.SeenSinceLastUpdate.First().IsCurrentEndUser.Should().BeTrue();
        dialog.SeenSinceLastUpdate.First().SeenBy.ActorId.Should().Contain("urn:altinn:person:identifier-ephemeral");
    }

    [E2EFact]
    public async Task Should_Return_Items_On_Simple_List()
    {
        // Arrange
        var sentinelTag = Guid.NewGuid().ToString();
        var controlTag = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.SearchTags = [new() { Value = sentinelTag }];
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.SearchTags = [new() { Value = controlTag }];
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelTag
            }, new(), ct),
            isSuccessful: r => r.Content?.Items.Count(x => x.Id == dialogId) == 1 &&
                               r.Content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();
        searchResult.Content.Items.Should().ContainSingle(x => x.Id == dialogId);
        searchResult.Content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Support_Pagination_With_Limit_And_ContinuationToken()
    {
        // Arrange - create 4 dialogs with a unique sentinel tag
        var sentinelTag = Guid.NewGuid().ToString();
        for (var i = 0; i < 4; i++)
        {
            await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
            {
                x.SearchTags = [new() { Value = sentinelTag }];
            });
        }

        // Verify that all 4 dialogs are searchable
        await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelTag,
                Limit = 4
            }, new(), ct),
            isSuccessful: r => r.Content?.Items.Count == 4,
            degradationMessage: "Search indexing speed is degraded.");

        // Act - first page with limit 2
        var firstPage = await Fixture.EnduserApi.V1.SearchDialogs(new()
        {
            Party = [E2EConstants.DefaultParty],
            Search = sentinelTag,
            Limit = 2
        }, new());

        // Assert first page
        firstPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        firstPage.Content.Should().NotBeNull();
        firstPage.Content.Items.Should().HaveCount(2);
        firstPage.Content.HasNextPage.Should().BeTrue();
        firstPage.Content.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        // Act - second page using continuation token
        var secondPage = await Fixture.EnduserApi.V1.SearchDialogs(new()
        {
            Party = [E2EConstants.DefaultParty],
            Search = sentinelTag,
            Limit = 2,
            ContinuationToken = firstPage.Content.ContinuationToken
        }, new());

        // Assert second page has different IDs
        secondPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        secondPage.Content.Should().NotBeNull();
        secondPage.Content.HasNextPage.Should().BeFalse();
        secondPage.Content.Items.Should().HaveCount(2);

        var firstPageIds = firstPage.Content.Items.Select(x => x.Id).ToList();
        var secondPageIds = secondPage.Content.Items.Select(x => x.Id).ToList();
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [E2EFact]
    public async Task Should_Return_400_When_Process_Is_Invalid()
    {
        // Act
        var response = await Fixture.EnduserApi.V1.SearchDialogs(new()
        {
            Party = [E2EConstants.DefaultParty],
            Process = "inval|d"
        }, new());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Return_400_When_SystemLabel_Is_Invalid()
    {
        // Refit cannot serialize invalid enum values for query params,
        // so this negative test must send a raw value.
        var token = await TestTokenGenerator.GenerateTokenAsync(
            TokenKind.EndUser,
            Fixture.Settings,
            TestContext.Current.CancellationToken);

        var uriBuilder = new UriBuilder(Fixture.Settings.DialogportenBaseUri)
        {
            Port = Fixture.Settings.WebAPiPort
        };
        uriBuilder.Path = $"{uriBuilder.Path.TrimEnd('/')}/api/v1/enduser/dialogs";
        uriBuilder.Query = $"party={Uri.EscapeDataString(E2EConstants.DefaultParty)}" +
                           $"&systemLabel={Uri.EscapeDataString("invalid")}";

        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Search_By_Title()
    {
        // Arrange
        var uniqueTitle = Guid.NewGuid().ToString();
        var controlTitle = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.Title = new()
            {
                Value = [new()
                {
                    Value = uniqueTitle,
                    LanguageCode = "nb"
                }]
            };
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.Title = new()
            {
                Value = [new()
                {
                    Value = controlTitle,
                    LanguageCode = "nb"
                }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueTitle
            }, new(), ct),
            isSuccessful: r => r.Content?.Items.Count(x => x.Id == dialogId) == 1 &&
                               r.Content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();
        searchResult.Content.Items.Should().ContainSingle(x => x.Id == dialogId);
        searchResult.Content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_AdditionalInfo()
    {
        // Arrange
        var uniqueAdditionalInfo = Guid.NewGuid().ToString();
        var controlAdditionalInfo = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.AdditionalInfo = new()
            {
                MediaType = "text/plain",
                Value = [new()
                {
                    Value = uniqueAdditionalInfo,
                    LanguageCode = "nb"
                }]
            };
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.AdditionalInfo = new()
            {
                MediaType = "text/plain",
                Value = [new()
                {
                    Value = controlAdditionalInfo,
                    LanguageCode = "nb"
                }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueAdditionalInfo
            }, new(), ct),
            isSuccessful: r => r.Content?.Items.Count(x => x.Id == dialogId) == 1 &&
                               r.Content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();
        searchResult.Content.Items.Should()
            .ContainSingle(x => x.Id == dialogId);
        searchResult.Content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_SenderName()
    {
        // Arrange
        var uniqueSenderName = Guid.NewGuid().ToString();
        var controlSenderName = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.SenderName = new()
            {
                Value = [new() { Value = uniqueSenderName, LanguageCode = "nb" }]
            };
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.SenderName = new()
            {
                Value = [new() { Value = controlSenderName, LanguageCode = "nb" }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1.SearchDialogs(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueSenderName
            }, new(), ct),
            isSuccessful: r => r.Content?.Items.Count(x => x.Id == dialogId) == 1 &&
                               r.Content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();
        searchResult.Content.Items.Should().ContainSingle(x => x.Id == dialogId);
        searchResult.Content.Items.Should().NotContain(x => x.Id == controlDialogId);
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
            ct => Fixture.EnduserApi.V1.SearchDialogs(new SearchDialogsQueryParams
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelLabel
            }, new(), ct),
            isSuccessful: searchResult => searchResult.Content?.Items.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);

        searchResult.Content.Should().NotBeNull();

        var dialog = searchResult.Content.Items.Single(x => x.Id == dialogId);
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
