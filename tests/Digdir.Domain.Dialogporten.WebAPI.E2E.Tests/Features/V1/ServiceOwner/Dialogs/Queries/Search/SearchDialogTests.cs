using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
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

    [E2EFact]
    public async Task Should_Return_Items_On_Simple_List()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var dialogIds = await Task.WhenAll(
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel),
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel),
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel));

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel]
            }, ct),
            isSuccessful: response => response.Content?.Items
                .Count(x => dialogIds.Contains(x.Id)) == dialogIds.Length,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();
        searchResult.Content.Items.Count(x => dialogIds
            .Contains(x.Id)).Should().Be(dialogIds.Length);
    }

    [E2EFact]
    public async Task Should_Support_Pagination_With_Limit_And_ContinuationToken()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var dialogIds = await Task.WhenAll(
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel),
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel),
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel),
            Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel));

        // Verify all 4 dialogs are indexed
        await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Limit = 4
            }, ct),
            isSuccessful: response => response.Content?.Items
                .Count(x => dialogIds.Contains(x.Id)) == dialogIds.Length,
            degradationMessage: "Search indexing speed is degraded.");

        // Act
        var firstPage = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            ServiceOwnerLabels = [sentinelLabel],
            Limit = 2
        });

        // Assert first page
        firstPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        firstPage.Content.Should().NotBeNull();

        var firstPageContent = firstPage.Content;
        firstPageContent.Items.Should().HaveCount(2);
        firstPageContent.HasNextPage.Should().BeTrue();
        firstPageContent.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        // Act
        var secondPage = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            ServiceOwnerLabels = [sentinelLabel],
            Limit = 2,
            ContinuationToken = firstPageContent.ContinuationToken
        });

        // Assert second page
        secondPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        secondPage.Content.Should().NotBeNull();

        var secondPageContent = secondPage.Content;
        secondPageContent.HasNextPage.Should().BeFalse();
        secondPageContent.Items.Should().HaveCount(2);

        var allIds = firstPageContent.Items
            .Select(x => x.Id)
            .Concat(secondPageContent.Items.Select(x => x.Id));

        allIds.Should().OnlyHaveUniqueItems();
    }

    [E2EFact]
    public async Task Should_Support_Custom_OrderBy()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var sharedDueAt = new DateTimeOffset(2028, 12, 7, 10, 13, 0, TimeSpan.Zero);
        var olderUpdatedAt = new DateTimeOffset(2024, 12, 7, 10, 13, 0, TimeSpan.Zero);
        var newerUpdatedAt = new DateTimeOffset(2025, 12, 7, 10, 13, 0, TimeSpan.Zero);

        var dueDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.Title = DialogTestData.CreateContentValue(Guid.NewGuid().ToString(), "nb");
            dialog.DueAt = new DateTimeOffset(2033, 12, 7, 10, 13, 0, TimeSpan.Zero);
            dialog.CreatedAt = olderUpdatedAt;
            dialog.UpdatedAt = olderUpdatedAt;
        });

        var updatedDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.DueAt = sharedDueAt;
            dialog.CreatedAt = newerUpdatedAt;
            dialog.UpdatedAt = newerUpdatedAt;
        });

        var lastDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.Title = DialogTestData.CreateContentValue(Guid.NewGuid().ToString(), "nb");
            dialog.DueAt = sharedDueAt;
            dialog.CreatedAt = olderUpdatedAt;
            dialog.UpdatedAt = olderUpdatedAt;
        });

        Guid[] expectedDescendingOrder =
        [
            dueDialogId,
            updatedDialogId,
            lastDialogId
        ];

        Guid[] expectedAscendingOrder =
        [
            updatedDialogId,
            lastDialogId,
            dueDialogId
        ];

        // Act
        var descendingOrderSearchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Limit = 3,
                OrderBy = "dueAt_desc,updatedAt_desc"
            }, ct),
            isSuccessful: response =>
                response.Content?.Items.Select(x => x.Id)
                    .SequenceEqual(expectedDescendingOrder) == true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert descending order
        descendingOrderSearchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        descendingOrderSearchResult.Content.Should().NotBeNull();
        descendingOrderSearchResult.Content.Items.Select(x => x.Id)
            .Should().Equal(expectedDescendingOrder);

        // Act
        var ascendingOrderSearchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Limit = 3,
                OrderBy = "dueAt_asc,updatedAt_desc"
            }, ct),
            isSuccessful: response =>
                response.Content?.Items.Select(x => x.Id)
                    .SequenceEqual(expectedAscendingOrder) == true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert ascending order
        ascendingOrderSearchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        ascendingOrderSearchResult.Content.Should().NotBeNull();
        ascendingOrderSearchResult.Content.Items.Select(x => x.Id)
            .Should().Equal(expectedAscendingOrder);
    }

    [E2EFact]
    public async Task Should_Search_By_Title()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var uniqueTitle = Guid.NewGuid().ToString();
        var controlTitle = Guid.NewGuid().ToString();

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.Title = DialogTestData.CreateContentValue(uniqueTitle, "nb");
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.Title = DialogTestData.CreateContentValue(controlTitle, "nb");
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                ServiceResource = [E2EConstants.DefaultServiceResource],
                EndUserId = E2EConstants.DefaultParty,
                Search = uniqueTitle
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count(x => x.Id == dialogId) == 1 &&
                content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle(x => x.Id == dialogId);
        content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_AdditionalInfo()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var uniqueAdditionalInfo = Guid.NewGuid().ToString();
        var controlAdditionalInfo = Guid.NewGuid().ToString();

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.AdditionalInfo = DialogTestData.CreateContentValue(
                uniqueAdditionalInfo,
                "nb",
                mediaType: "text/plain");
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.AdditionalInfo = DialogTestData.CreateContentValue(
                controlAdditionalInfo,
                "nb",
                mediaType: "text/plain");
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                ServiceResource = [E2EConstants.DefaultServiceResource],
                EndUserId = E2EConstants.DefaultParty,
                Search = uniqueAdditionalInfo
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count(x => x.Id == dialogId) == 1 &&
                content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle(x => x.Id == dialogId);
        content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_SenderName()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var uniqueSenderName = Guid.NewGuid().ToString();
        var controlSenderName = Guid.NewGuid().ToString();

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.SenderName = DialogTestData.CreateContentValue(uniqueSenderName, "nb");
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Content.SenderName = DialogTestData.CreateContentValue(controlSenderName, "nb");
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                ServiceResource = [E2EConstants.DefaultServiceResource],
                EndUserId = E2EConstants.DefaultParty,
                Search = uniqueSenderName
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count(x => x.Id == dialogId) == 1 &&
                content.Items.All(x => x.Id != controlDialogId),
           degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle(x => x.Id == dialogId);
        content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2ETheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Filter_By_ServiceResource(bool withEndUserId)
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.ServiceResource = E2EConstants.AlternateServiceResource;
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                ServiceResource = [E2EConstants.AlternateServiceResource],
                EndUserId = withEndUserId ? E2EConstants.DefaultParty : null!
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
        content.Items.Single().ServiceResource.Should()
            .Be(E2EConstants.AlternateServiceResource);
    }

    [E2EFact]
    public async Task Should_Return_Empty_Items_When_EndUserId_Has_No_Authorizations()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var unauthorizedEndUserId = $"{NorwegianPersonIdentifier.PrefixWithSeparator}08895699684";

        _ = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.ServiceResource = E2EConstants.AlternateServiceResource;
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            ServiceOwnerLabels = [sentinelLabel],
            ServiceResource = [E2EConstants.AlternateServiceResource],
            EndUserId = unauthorizedEndUserId
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Items.Should().BeNull();
    }
}
