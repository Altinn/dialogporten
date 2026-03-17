using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.ErrorResponse;

[Collection(nameof(WebApiTestCollectionFixture))]
public class ErrorResponseSnapshotTests(WebApiE2EFixture fixture) : ErrorResponseSnapshotTestBase(fixture)
{
    [E2EFact]
    public async Task Should_Return_404_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EnduserApi.GetDialog(nonExistentDialogId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerifyErrorResponse(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_404_Error_Response_For_Activities_On_NonExistent_Dialog()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EnduserApi
            .V1EndUserDialogsQueriesSearchActivitiesDialogActivity(nonExistentDialogId, new V1EndUserCommon_AcceptedLanguages());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerifyErrorResponse(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_404_Error_Response_For_NonExistent_Activity()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var nonExistentActivityId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EnduserApi
            .V1EndUserDialogsQueriesGetActivityDialogActivity(dialogId, nonExistentActivityId, new V1EndUserCommon_AcceptedLanguages());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerifyErrorResponse(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_410_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);

        // Act
        var response = await Fixture.EnduserApi
            .V1EndUserDialogsQueriesGetDialog(dialogId, new V1EndUserCommon_AcceptedLanguages());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        await VerifyErrorResponse(response.Error!.Content!);
    }

    [E2EFact]
    public async Task Should_Return_403_For_Inadequate_Auth_Level()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
            // This serviceResource requires auth level 4, default user has level 3
            x.ServiceResource = "urn:altinn:resource:ttd-dialogporten-transmissions-test");

        // Act
        var response = await Fixture.EnduserApi
            .V1EndUserDialogsQueriesGetDialog(dialogId, new V1EndUserCommon_AcceptedLanguages());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerifyErrorResponse(response.Error!.Content!);
    }
}
