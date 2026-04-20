using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogFilterTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
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

}
