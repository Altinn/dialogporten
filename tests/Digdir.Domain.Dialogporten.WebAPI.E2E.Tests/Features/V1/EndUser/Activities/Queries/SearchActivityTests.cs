using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Activities.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Search_Activities()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId);

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchActivitiesDialogActivity(
            dialogId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Activity content was null.");
        content.Should().ContainSingle().Which.Id.Should().Be(activityId);
    }
}
