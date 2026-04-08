using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogFilterTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_SeenSinceLastUpdate_When_Dialog_Has_Been_Viewed()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Trigger seen log by getting the dialog
        var getResponse = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.GetDialog(dialogId, cancellationToken: ct),
            isSuccessful: r => r.IsSuccessStatusCode,
            degradationMessage: "Dialog get is degraded.");

        getResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty]
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.FirstOrDefault(x => x.Id == dialogId)
                ?.SeenSinceLastUpdate?.Count > 0,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        var dialog = searchResult.Content!.Items!.Single(x => x.Id == dialogId);
        dialog.SeenSinceLastUpdate.Should().HaveCount(1);
        dialog.SeenSinceLastUpdate.First().IsCurrentEndUser.Should().BeTrue();
        dialog.SeenSinceLastUpdate.First().SeenBy.ActorId.Should().Contain("urn:altinn:person:identifier-ephemeral");
    }

    [E2EFact]
    public async Task Should_Return_Items_On_Simple_List()
    {
        // Arrange
        var sentinelTag = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.SearchTags = [new() { Value = sentinelTag }];
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = sentinelTag
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().ContainSingle(x => x.Id == dialogId);
    }

    [E2EFact]
    public async Task Should_Filter_By_ExtendedStatus()
    {
        // Arrange
        var extendedStatus1 = $"status:{Guid.NewGuid()}";
        var extendedStatus2 = $"status:{Guid.NewGuid()}";

        var dialogId1 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.ExtendedStatus = extendedStatus1;
        });

        var dialogId2 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.ExtendedStatus = extendedStatus2;
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                ExtendedStatus = [extendedStatus1, extendedStatus2]
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Count(x => x.Id == dialogId1 || x.Id == dialogId2) == 2,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().Contain(x => x.Id == dialogId1);
        searchResult.Content!.Items.Should().Contain(x => x.Id == dialogId2);
    }

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
            ContinuationToken = JsonSerializer.Deserialize<ContinuationTokenSetOfTOrderDefinitionAndTTarget>(
                $"\"{firstPage.Content.ContinuationToken}\"")!
        }, new());

        // Assert second page has different IDs
        secondPage.ShouldHaveStatusCode(HttpStatusCode.OK);
        secondPage.Content!.Items.Should().HaveCount(2);

        var firstPageIds = firstPage.Content.Items!.Select(x => x.Id).ToList();
        var secondPageIds = secondPage.Content.Items!.Select(x => x.Id).ToList();
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [E2EFact]
    public async Task Should_Filter_By_ServiceResource()
    {
        // Arrange - create a dialog with a non-default service resource
        const string auxResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.ServiceResource = auxResource;
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                ServiceResource = [auxResource]
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().Contain(x => x.Id == dialogId);
        searchResult.Content.Items!.Single(x => x.Id == dialogId)
            .ServiceResource.Should().Be(auxResource);
    }

    [E2EFact]
    public async Task Should_Filter_By_Process()
    {
        // Arrange
        const string process = "urn:test:process:1";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Process = process;
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Process = process
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().Contain(x => x.Id == dialogId);
        searchResult.Content.Items!.Single(x => x.Id == dialogId)
            .Process.Should().Be(process);
    }

    [E2EFact]
    public async Task Should_Return_400_When_Process_Is_Invalid()
    {
        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
        {
            Party = [E2EConstants.DefaultParty],
            Process = "inval|d"
        }, new());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Filter_By_SystemLabel()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Set system label to Bin via service owner API
        var setLabelResponse = await Fixture.ServiceownerApi.SetSystemLabel(
            dialogId,
            E2EConstants.DefaultEndUserSsn,
            x => x.AddLabels = [Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel.Bin]);
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                SystemLabel = [DialogEndUserContextsEntities_SystemLabel.Bin]
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        var dialog = searchResult.Content!.Items!.Single(x => x.Id == dialogId);
        dialog.EndUserContext.SystemLabels.Should().Contain(DialogEndUserContextsEntities_SystemLabel.Bin);
    }

    [E2EFact]
    public async Task Should_Return_400_When_SystemLabel_Is_Invalid()
    {
        // Act - use raw HTTP to pass an invalid system label string
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
        {
            Party = [E2EConstants.DefaultParty],
            SystemLabel = [(DialogEndUserContextsEntities_SystemLabel)999]
        }, new());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Search_By_Title()
    {
        // Arrange
        var uniqueTitle = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.Title = new()
            {
                Value = [new() { Value = uniqueTitle, LanguageCode = "nb" }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueTitle
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().ContainSingle(x => x.Id == dialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_AdditionalInfo()
    {
        // Arrange
        var uniqueAdditionalInfo = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.AdditionalInfo = new()
            {
                MediaType = "text/plain",
                Value = [new() { Value = uniqueAdditionalInfo, LanguageCode = "nb" }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueAdditionalInfo
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().ContainSingle(x => x.Id == dialogId);
    }

    [E2EFact]
    public async Task Should_Search_By_SenderName()
    {
        // Arrange
        var uniqueSenderName = Guid.NewGuid().ToString();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.Content.SenderName = new()
            {
                Value = [new() { Value = uniqueSenderName, LanguageCode = "nb" }]
            };
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
            {
                Party = [E2EConstants.DefaultParty],
                Search = uniqueSenderName
            }, new(), ct),
            isSuccessful: r => r.Content?.Items?.Any(x => x.Id == dialogId) is true,
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content!.Items.Should().ContainSingle(x => x.Id == dialogId);
    }
}
