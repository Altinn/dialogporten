using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ActivityType = Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;
using ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType;

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

    [E2EFact]
    public async Task Search_Activities_Verify_Snapshot()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(x =>
        {
            x.AddTransmission(t => t.Id = transmissionId);
            x.Activities = [
                new()
                {
                    Type = ActivityType.DialogOpened,
                    ExtendedType = new("uri:dialog-opened"),
                    PerformedBy = new()
                    {
                        ActorType = ActorType.PartyRepresentative,
                        ActorId = E2EConstants.DefaultParty
                    }
                },
                new()
                {
                    Type = ActivityType.Information,
                    Description = [ new() { LanguageCode = "en", Value = "Test activity" }],
                    PerformedBy = new()
                    {
                        ActorType = ActorType.ServiceOwner
                    }
                },
                new()
                {
                    Type = ActivityType.TransmissionOpened,
                    TransmissionId = transmissionId,
                    PerformedBy = new()
                    {
                        ActorType = ActorType.PartyRepresentative,
                        ActorId = E2EConstants.DefaultParty
                    }
                }
            ];
        });

        await Fixture.EnduserApi.GetDialog(dialogId);

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchActivitiesDialogActivity(
            dialogId,
            new V1EndUserCommon_AcceptedLanguages(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
