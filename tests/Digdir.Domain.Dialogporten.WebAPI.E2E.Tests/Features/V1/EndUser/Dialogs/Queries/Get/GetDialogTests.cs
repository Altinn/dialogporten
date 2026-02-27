using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Dialog_By_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var languages = new V1EndUserCommon_AcceptedLanguages();
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Id.Should().Be(dialogId);
    }

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
        seenEntry.SeenBy.ActorId.Should().Contain("urn:altinn:person:identifier-ephemeral");
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
}
