using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Xunit;
using ProblemDetails = Refit.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Create_Activity()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var request = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorName = null!,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };

        // Act
        var response = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = response.Content ?? throw new InvalidOperationException("Expected a body");
        Guid.Parse(content.Replace("\"", ""));
    }

    [E2EFact]
    public async Task Should_Be_Able_To_Create_Activity_When_IfMatch_DialogRevision_Is_Unchanged()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogRes = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(dialogId, null!);
        dialogRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var dialogReq = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorName = null!,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };
        var ifMatch = dialogRes.Content!.Revision;

        // Act
        var response = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, dialogReq, ifMatch);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = response.Content ?? throw new InvalidOperationException("Expected a body");
        Guid.Parse(content.Replace("\"", ""));
    }

    [E2EFact]
    public async Task Should_NotBe_Able_To_Create_Activity_When_IfMatch_DialogRevision_Is_Changed()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogRes = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(dialogId, null!);
        dialogRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var dialogReq = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorName = null!,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };

        // Act
        var response = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, dialogReq, Guid.CreateVersion7());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Should().BeNull();
    }

    [E2EFact]
    public async Task Should_Not_Be_Able_To_Create_Activity_On_Another_Users_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var request = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorName = null!,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };
        using var _ = Fixture.UseServiceOwnerTokenOverrides("964951284", "hko");

        // Act
        var response = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Should().BeNull();
    }

    [E2EFact]
    public async Task Should_Not_Be_Able_To_Create_The_Same_Activity_Twice()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var request = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = Guid.Parse("019c322e-855f-7837-8255-9c922a84eff5"),
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorName = null!,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };

        // Act
        var response1 = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        var response2 = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = response1.Content ?? throw new InvalidOperationException("Expected a body");
        Guid.Parse(content.Replace("\"", ""));

        response2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response2.Content.Should().BeNull();
        var errorBody = await response2.Error!.GetContentAsAsync<ProblemDetails>(); // Todo: Swap to class from SDK when we can generate the correct class
        errorBody.Should().NotBeNull();
    }
}
