using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Populate_SeenLog_After_Get()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act

        // Get a dialog to trigger a dialogSeenEvent
        var getDialogResponse = await Fixture.EnduserApi.GetDialog(dialogId);

        getDialogResponse.IsSuccessful.Should().BeTrue();
        getDialogResponse.Content.Should().NotBeNull();

        // Since seenlog is created async we except 0 seenlogs on first get
        getDialogResponse.Content.SeenSinceLastUpdate.Should().BeNull();

        // Seen log is created async so we retry until a seen log is created
        var response = await E2ERetryPolicies.RetryUntilAsync(
            operation: ct => Fixture.EnduserApi.GetDialog(dialogId, cancellationToken: ct),
            isSuccessful: result => result.IsSuccessful && result.Content.SeenSinceLastUpdate.Count == 1,
            degradationMessage: "SeenLog creation speed is degraded");

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.SeenSinceLastUpdate.Should().HaveCount(1);

        var seenEntry = content.SeenSinceLastUpdate.Single();
        seenEntry.SeenBy.ActorId.Should().Contain(NorwegianPersonIdentifier.HashPrefix);
        seenEntry.IsCurrentEndUser.Should().BeTrue();
    }

    [E2EFact]
    public async Task Should_Have_Authorized_GuiActions_With_Real_Urls()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.GuiActions.Should().HaveCount(2);

        var firstAction = content.GuiActions.First();
        firstAction.IsAuthorized.Should().BeTrue();
        firstAction.Url.ToString().Should().Contain("https://");

        var secondAction = content.GuiActions.Last();
        secondAction.Prompt.Should().NotBeEmpty();
        secondAction.HttpMethod.Should().Be(Http_HttpVerb.POST);
    }

    [E2EFact]
    public async Task Should_Have_Unauthorized_ApiActions_With_Default_Urls()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.ApiActions.Should().HaveCount(1);

        var apiAction = content.ApiActions.Single();
        apiAction.IsAuthorized.Should().BeFalse();
        apiAction.Endpoints.Should().NotBeEmpty();

        apiAction.Endpoints.Should().AllSatisfy(endpoint =>
            endpoint.Url.ToString().Should()
                .Be(Constants.UnauthorizedUri.ToString()));

    }

    [E2EFact]
    public async Task Should_Have_Correct_Transmission_Authorization()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Transmissions.Should().NotBeEmpty();

        var availableExternalResource = content.Transmissions
            .Single(t => t.AuthorizationAttribute == "urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence");
        availableExternalResource.IsAuthorized.Should().BeTrue();

        var unavailableExternalResource = content.Transmissions
            .Single(t => t.AuthorizationAttribute == "urn:altinn:resource:ttd-altinn-events-automated-tests");
        unavailableExternalResource.IsAuthorized.Should().BeFalse();

        var unavailableSubresource = content.Transmissions
            .Single(t => t.AuthorizationAttribute == "someunavailablesubresource");
        unavailableSubresource.IsAuthorized.Should().BeFalse();
    }

    [E2EFact]
    public async Task Should_Return_404_After_Purge()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var purgeResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        purgeResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_Forbidden_With_Inadequate_Auth_Level()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
            // This serviceResource requires auth level 4, default user has level 3
            x.ServiceResource = "urn:altinn:resource:ttd-dialogporten-transmissions-test");

        // Act
        var response = await Fixture.EnduserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        response.Error!.Content.Should().Contain(Constants.AltinnAuthLevelTooLow);
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Get_Dialog_Verify_Snapshot()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var getDialogResult = await E2ERetryPolicies.RetryUntilAsync(
            operation: ct => Fixture.EnduserApi.GetDialog(dialogId, cancellationToken: ct),
            isSuccessful: result => result is { IsSuccessful: true, Content.SeenSinceLastUpdate.Count: 1 },
            degradationMessage: "Seen log creation delayed");

        // Assert
        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(getDialogResult.Content));
    }
}
