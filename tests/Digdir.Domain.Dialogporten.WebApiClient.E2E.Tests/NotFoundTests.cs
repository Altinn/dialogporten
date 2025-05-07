using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

public class NotFoundTests : TestBed<AuthorizedE2EFixture>
{
    private readonly IServiceownerApi _serviceownerApi;
    // private readonly Guid _sentinelId = Guid.NewGuid();

    public NotFoundTests(ITestOutputHelper testOutputHelper, AuthorizedE2EFixture fixture) : base(testOutputHelper,
        fixture)
    {
        _serviceownerApi = fixture.GetService<IServiceownerApi>(_testOutputHelper)!;
    }

    //
    // public new async ValueTask DisposeAsync()
    // {
    //     Console.WriteLine("[CLEANUP] Disposing E2EFixture 🧹");
    //
    //     Console.WriteLine(_sentinelId.ToString());
    //     // 🔥 Do any teardown work here
    //     await Task.Delay(500); // Simulate async cleanup
    //
    //     await base.DisposeAsync(); // Ensure base cleanup runs
    // }

    [ConditionalSkipFact]
    public async Task Get_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var getDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsGetDialog(dialogId, null!, CancellationToken.None);

        getDialogResponse.IsSuccessful.Should().BeFalse();
        getDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Get_Transmission_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var transmissionId = Guid.NewGuid();

        var getTransmissionResponse = await _serviceownerApi
            .V1ServiceOwnerDialogTransmissionsGetDialogTransmission(dialogId, transmissionId,
                CancellationToken.None);

        getTransmissionResponse.IsSuccessful.Should().BeFalse();
        getTransmissionResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Get_Transmission_With_Valid_DialogId_And_InvalidTransmissionId_Should_Return_NotFound()
    {
        var createDialogResult = await _serviceownerApi.V1ServiceOwnerDialogsCreateDialog(
            new V1ServiceOwnerDialogsCommandsCreate_Dialog
            {
                ServiceResource = "urn:altinn:resource:super-simple-service",
                Party = "urn:altinn:person:identifier-no:08895699684",
                SearchTags = [],
                Attachments = [],
                ApiActions = [],
                GuiActions = [],
                Transmissions = [],
                Activities = [],
                Content = new()
                {
                    Title = new() { MediaType = "text/plain", Value = [new() { LanguageCode = "nb", Value = "New Title" }] },
                    Summary = new() { MediaType = "text/plain", Value = [new() { LanguageCode = "nb", Value = "New Summary" }] },
                }
            },
            CancellationToken.None);

        createDialogResult.IsSuccessful.Should().BeTrue();

        var dialogId = Guid.Parse(createDialogResult.Content?.Replace("\"", "")!);
        var transmissionId = Guid.NewGuid();

        var getTransmissionResponse = await _serviceownerApi
            .V1ServiceOwnerDialogTransmissionsGetDialogTransmission(dialogId, transmissionId,
                CancellationToken.None);

        getTransmissionResponse.IsSuccessful.Should().BeFalse();
        getTransmissionResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Search_Transmission_With_Invalid_TransmissionId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var searchTransmissionResponse = await _serviceownerApi
            .V1ServiceOwnerDialogTransmissionsSearchDialogTransmission(dialogId, CancellationToken.None);

        searchTransmissionResponse.IsSuccessful.Should().BeFalse();
        searchTransmissionResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Get_Activity_With_Invalid_ActivityId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var activityId = Guid.NewGuid();

        var getActivityResponse = await _serviceownerApi
            .V1ServiceOwnerDialogActivitiesGetDialogActivity(dialogId, activityId, CancellationToken.None);

        getActivityResponse.IsSuccessful.Should().BeFalse();
        getActivityResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Search_Activity_With_Invalid_ActivityId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var searchActivityResponse = await _serviceownerApi
            .V1ServiceOwnerDialogActivitiesSearchDialogActivity(dialogId, CancellationToken.None);

        searchActivityResponse.IsSuccessful.Should().BeFalse();
        searchActivityResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Get_SeenLog_With_Invalid_SeenLogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var seenLogId = Guid.NewGuid();

        var getSeenLogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogSeenLogsGetDialogSeenLog(dialogId, seenLogId, CancellationToken.None);

        getSeenLogResponse.IsSuccessful.Should().BeFalse();
        getSeenLogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Patch_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var patchDocument = new List<JsonPatchOperations_Operation>
        {
            new()
            {
                Op = "replace",
                Path = "/title",
                Value = "New Title"
            }
        };

        var patchDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsPatchDialog(dialogId, patchDocument, null, CancellationToken.None);

        patchDialogResponse.IsSuccessful.Should().BeFalse();
        patchDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Update_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var updateDialogRequest = new V1ServiceOwnerDialogsCommandsUpdate_Dialog
        {
            SearchTags = [],
            Attachments = [],
            ApiActions = [],
            GuiActions = [],
            Transmissions = [],
            Activities = [],
            Content = new()
            {
                Title = new() { MediaType = "text/plain", Value = [new() { LanguageCode = "nb", Value = "New Title" }] },
                Summary = new() { MediaType = "text/plain", Value = [new() { LanguageCode = "nb", Value = "New Summary" }] },
            }
        };

        var updateDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsUpdateDialog(dialogId, updateDialogRequest, null, CancellationToken.None);

        updateDialogResponse.IsSuccessful.Should().BeFalse();
        updateDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Purge_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var purgeDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsPurgeDialog(dialogId, null, CancellationToken.None);

        purgeDialogResponse.IsSuccessful.Should().BeFalse();
        purgeDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Delete_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var deleteDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsDeleteDialog(dialogId, null, CancellationToken.None);

        deleteDialogResponse.IsSuccessful.Should().BeFalse();
        deleteDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
