using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ActivityType = Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;
using ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Activities.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Search_Activities_Verify_Snapshot()
    {
        // Arrange
        var transmissionId = DialogTestData.NewUuidV7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.AddTransmission(transmission => transmission.Id = transmissionId);
            dialog.Activities =
            [
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
                    Description =
                    [
                        new()
                        {
                            LanguageCode = "en",
                            Value = "Test activity"
                        },
                        new()
                        {
                            LanguageCode = "nb",
                            Value = "Test-aktivitet"
                        }
                    ],
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

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchActivitiesDialogActivity(
            dialogId,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Should().HaveCount(3);
        response.Content.Should().Contain(x =>
            x.Type == ActivityType.TransmissionOpened && x.TransmissionId == transmissionId);

        await JsonSnapshotVerifier.VerifyJsonSnapshot(
            JsonSerializer.Serialize(response.Content));
    }
}
