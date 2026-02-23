using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;
using static Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateActivityTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Create_Activity_As_Service_Owner()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId);

        // Assert
        activityId.Should().NotBe(Guid.Empty);
    }

    [E2EFact]
    public async Task Should_Create_Activity_As_Admin()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(
            scopes: E2EConstants.ServiceOwnerScopes + " " + AuthorizationScope.ServiceOwnerAdminScope
        );
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId);

        // Assert
        activityId.Should().NotBe(Guid.Empty);
    }

    [E2EFact]
    public async Task Should_Be_Able_To_Create_Activity_When_IfMatch_DialogRevision_Is_Unchanged()
    {
        // Arrange
        var createDialogResult = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(
                DialogTestData.CreateSimpleDialog());

        var dialogId = createDialogResult.Content.ToGuid();
        var ifMatch = createDialogResult.Headers.ETagToGuid();

        // Act
        var activityId = await Fixture.ServiceownerApi.CreateSimpleActivityAsync(dialogId, ifMatch);

        // Assert
        activityId.Should().NotBe(Guid.Empty);
    }

    [E2EFact]
    public async Task Should_Not_Be_Able_To_Create_Activity_When_IfMatch_DialogRevision_Is_Changed()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogReq = DialogTestData.CreateSimpleActivity();

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
        var request = DialogTestData.CreateSimpleActivity();
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
    public async Task Should_Not_Be_Able_To_Create_Activity_On_Deleted_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);
        var request = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = PartyRepresentative,
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
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        response.Content.Should().BeNull();
    }

    [E2EFact]
    public async Task Should_Be_Able_To_Create_Activity_On_Deleted_Dialog_If_Admin()
    {
        // Arrange
        using var _ = Fixture.UseServiceOwnerTokenOverrides(
            scopes: E2EConstants.ServiceOwnerScopes + " " + AuthorizationScope.ServiceOwnerAdminScope
        );
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);
        var request = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Id = null,
            CreatedAt = null,
            ExtendedType = new Uri("http://localhost"),
            Type = DialogCreated,
            TransmissionId = null,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = PartyRepresentative,
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
        var guid = Guid.Parse(content.Replace("\"", ""));
        guid.Should().NotBe(Guid.Empty);
    }

    [E2EFact]
    public async Task Should_Not_Be_Able_To_Create_The_Same_Activity_Twice()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var activityId = Guid.CreateVersion7();
        var request = DialogTestData.CreateSimpleActivity(activity => activity.Id = activityId);

        // Act
        var response1 = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        var response2 = await Fixture
            .ServiceownerApi
            .V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, request, null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response1.Content.ToGuid();

        response2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response2.Content.Should().BeNull();
        response2.Error!.Content.Should().Contain($"Key ('Id')=({activityId}) already exists.");
    }
}
