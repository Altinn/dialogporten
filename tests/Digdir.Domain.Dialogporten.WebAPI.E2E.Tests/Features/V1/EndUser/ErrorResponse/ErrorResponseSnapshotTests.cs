using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.ErrorResponse;

[Collection(nameof(WebApiTestCollectionFixture))]
public class ErrorResponseSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private static readonly JsonSerializerOptions ProblemDetailsSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [E2EFact]
    public async Task Should_Return_404_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EndUserApi.GetDialog(nonExistentDialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await VerifyProblemDetailsSnapshot(response);
    }

    [E2EFact]
    public async Task Should_Return_404_Error_Response_For_Activities_On_NonExistent_Dialog()
    {
        // Arrange
        var nonExistentDialogId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EndUserApi
            .V1.SearchDialogActivities(nonExistentDialogId, new AcceptedLanguages());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await VerifyProblemDetailsSnapshot(response);
    }

    [E2EFact]
    public async Task Should_Return_404_Error_Response_For_NonExistent_Activity()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var nonExistentActivityId = Guid.CreateVersion7();

        // Act
        var response = await Fixture.EndUserApi
            .V1.GetDialogActivity(dialogId, nonExistentActivityId, new AcceptedLanguages());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        await VerifyProblemDetailsSnapshot(response);
    }

    [E2EFact]
    public async Task Should_Return_410_Error_Response_In_ProblemDetails_Format()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, null);

        // Act
        var response = await Fixture.EndUserApi
            .V1.GetDialog(dialogId, new AcceptedLanguages());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Gone);
        await VerifyProblemDetailsSnapshot(response);
    }

    [E2EFact]
    public async Task Should_Return_403_For_Inadequate_Auth_Level()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
            // This serviceResource requires auth level 4, default user has level 3
            x.ServiceResource = "urn:altinn:resource:ttd-dialogporten-transmissions-test");

        // Act
        var response = await Fixture.EndUserApi
            .V1.GetDialog(dialogId, new AcceptedLanguages());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        await VerifyProblemDetailsSnapshot(response);
    }

    private static async Task VerifyProblemDetailsSnapshot(
        Refit.IApiResponse response,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string sourceFile = "")
    {
        var problemDetails = await response.Error!
            .GetContentAsAsync<ProblemDetails>();

        problemDetails.Should().NotBeNull();

        var jsonProblemDetails = JsonSerializer
            .Serialize(problemDetails,
                ProblemDetailsSerializerOptions);

        await VerifyJsonSnapshot(
            jsonProblemDetails,
            callerMemberName: callerMemberName,
            sourceFile: sourceFile);
    }
}
