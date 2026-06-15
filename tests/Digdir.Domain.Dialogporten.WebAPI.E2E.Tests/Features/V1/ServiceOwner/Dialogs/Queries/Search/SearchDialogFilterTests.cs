using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogFilterTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Filter_By_ExtendedStatus()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var extendedStatus1 = $"status:{Guid.NewGuid()}";
        var extendedStatus2 = $"status:{Guid.NewGuid()}";
        var controlExtendedStatus = $"status:{Guid.NewGuid()}";

        var dialogId1 = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.ExtendedStatus = extendedStatus1;
        });

        var dialogId2 = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.ExtendedStatus = extendedStatus2;
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.ExtendedStatus = controlExtendedStatus;
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                ExtendedStatus = [extendedStatus1, extendedStatus2]
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count == 2 &&
                content.Items.Any(x => x.Id == dialogId1) &&
                content.Items.Any(x => x.Id == dialogId2) &&
                content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(x => x.Id == dialogId1);
        content.Items.Should().Contain(x => x.Id == dialogId2);
        content.Items.Should().NotContain(x => x.Id == controlDialogId);
    }

    [E2EFact]
    public async Task Should_Filter_By_Party()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var auxParty = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{E2EConstants.GetDefaultServiceOwnerOrgNr()}";

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Party = auxParty;
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel);

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Party = [auxParty]
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count == 1 &&
                content.Items.Single().Id == dialogId &&
                content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle();
        content.Items.Single().Id.Should().Be(dialogId);
        content.Items.Single().Party.Should().Be(auxParty);
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
    public async Task Should_Filter_By_ExcludeApiOnly()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();

        var apiOnlyDialogId = await Fixture.ServiceownerApi
            .CreateSearchDialogAsync(sentinelLabel, dialog => dialog.IsApiOnly = true);
        var regularDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel);

        // Act: by default, search includes API-only dialogs. Wait until both are indexed.
        var includeResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel]
            }, ct),
            isSuccessful: response =>
                response.Content is { Items.Count: 2 } content &&
                content.Items.Any(x => x.Id == apiOnlyDialogId) &&
                content.Items.Any(x => x.Id == regularDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        includeResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        includeResult.Content.Should().NotBeNull();
        includeResult.Content.Items.Should().Contain(x => x.Id == apiOnlyDialogId && x.IsApiOnly);
        includeResult.Content.Items.Should().Contain(x => x.Id == regularDialogId && !x.IsApiOnly);

        // Act: excluding API-only dialogs returns only the regular dialog.
        var excludeResult = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            ServiceOwnerLabels = [sentinelLabel],
            ExcludeApiOnly = true
        });

        // Assert
        excludeResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        excludeResult.Content.Should().NotBeNull();
        excludeResult.Content.Items.Should().ContainSingle();
        excludeResult.Content.Items.Single().Id.Should().Be(regularDialogId);
        excludeResult.Content.Items.Should().OnlyContain(x => !x.IsApiOnly);
    }

    [E2EFact]
    public async Task Should_Return_400_When_Process_Is_Invalid()
    {
        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
        {
            Process = "inval|d"
        });

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [E2EFact]
    public async Task Should_Filter_By_Process()
    {
        // Arrange
        var sentinelLabel = Guid.NewGuid().ToString();
        var process = $"urn:test:process:{Guid.NewGuid()}";
        var controlProcess = $"urn:test:process:{Guid.NewGuid()}";

        var dialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Process = process;
        });

        var controlDialogId = await Fixture.ServiceownerApi.CreateSearchDialogAsync(sentinelLabel, dialog =>
        {
            dialog.Process = controlProcess;
        });

        // Act
        var searchResult = await E2ERetryPolicies.RetryUntilAsync(
            ct => Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(new()
            {
                ServiceOwnerLabels = [sentinelLabel],
                Process = process
            }, ct),
            isSuccessful: response =>
                response.Content is { } content &&
                content.Items.Count == 1 &&
                content.Items.Single().Id == dialogId &&
                content.Items.All(x => x.Id != controlDialogId),
            degradationMessage: "Search indexing speed is degraded.");

        // Assert
        searchResult.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResult.Content.Should().NotBeNull();

        var content = searchResult.Content;
        content.Items.Should().ContainSingle();
        content.Items.Single().Id.Should().Be(dialogId);
        content.Items.Single().Process.Should().Be(process);
    }
}
