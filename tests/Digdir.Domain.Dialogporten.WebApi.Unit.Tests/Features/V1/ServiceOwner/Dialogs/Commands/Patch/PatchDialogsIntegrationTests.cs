using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Patch;

public class PatchDialogsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PatchDialogsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Patch_RemoveSearchTags_WhenDialogHasNoSearchTags_ShouldReturn204()
    {
        // Arrange - Create a dialog without search tags
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createCommand.Dto.SearchTags = []; // Empty list

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Get the dialog to retrieve the revision
        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        // Patch document to remove searchTags
        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/searchTags"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert - Should not return 500, should return 204 No Content
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }

    [Fact]
    public async Task Patch_RemoveAttachments_WhenDialogHasNoAttachments_ShouldReturn204()
    {
        // Arrange - Create a dialog without attachments
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createCommand.Dto.Attachments = []; // Empty list

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/attachments"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }

    [Fact]
    public async Task Patch_RemoveGuiActions_WhenDialogHasNoGuiActions_ShouldReturn204()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createCommand.Dto.GuiActions = []; // Empty list

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/guiActions"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }

    [Fact]
    public async Task Patch_RemoveApiActions_WhenDialogHasNoApiActions_ShouldReturn204()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createCommand.Dto.ApiActions = []; // Empty list

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/apiActions"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }

    [Fact]
    public async Task Patch_RemoveTransmissions_WhenDialogHasOnlyRequiredTransmission_ShouldHandleGracefully()
    {
        // Arrange - Dialog must have at least one transmission
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/transmissions"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert - Should not return 500 (might be 400 due to validation, but not 500)
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }

    [Fact]
    public async Task Patch_RemoveActivities_WhenDialogHasNoActivities_ShouldReturn204()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createCommand.Dto.Activities = []; // Empty list

        var createResponse = await _client.PostAsJsonAsync("/api/v1/serviceowner/dialogs", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var dialogId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/serviceowner/dialogs/{dialogId}");
        getResponse.EnsureSuccessStatusCode();
        var dialog = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = dialog.GetProperty("revision").GetGuid();

        var patchDocument = new[]
        {
            new
            {
                op = "remove",
                path = "/activities"
            }
        };

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/serviceowner/dialogs/{dialogId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Add("If-Match", revision.ToString());

        // Act
        var patchResponse = await _client.SendAsync(patchRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Cleanup
        await _client.PostAsync($"/api/v1/serviceowner/dialogs/{dialogId}/actions/purge", null);
    }
}
