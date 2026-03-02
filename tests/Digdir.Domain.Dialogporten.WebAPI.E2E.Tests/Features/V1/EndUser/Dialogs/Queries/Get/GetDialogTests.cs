using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;
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
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
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
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
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
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.ApiActions.Should().HaveCount(1);

        var apiAction = content.ApiActions.Single();
        apiAction.IsAuthorized.Should().BeFalse();
        apiAction.Endpoints.Should().NotBeEmpty();

        foreach (var endpoint in apiAction.Endpoints)
        {
            endpoint.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString());
        }
    }

    [E2EFact]
    public async Task Should_Have_Correct_Transmission_Authorization()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
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
        purgeResponse.IsSuccessful.Should().BeTrue();

        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
