using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Activities.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Activity_By_Id()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId, null, x =>
        {
            x.Type = Information;
            x.Description =
            [
                new()
                {
                    LanguageCode = "en",
                    Value = "Test activity"
                }
            ];
        });

        // Act
        var response = await Fixture.EnduserApi.V1.GetDialogActivity(
            dialogId,
            activityId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Activity content was null.");
        content.Id.Should().Be(activityId);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
