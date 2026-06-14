using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Refit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.ServiceOwnerContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class ServiceOwnerLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_All_Labels_And_ServiceOwner_Context_Revision()
    {
        // Arrange
        string[] labels = ["label1", "label2"];
        var dialogId = await CreateDialogWithLabels(labels);

        // Act
        var response = await GetServiceOwnerLabels(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();

        // +1 for the sentinel label from EphemeralDialogDecorator
        response.Content.Should().HaveCount(labels.Length + 1);
        response.Content.Select(x => x.Value).Should().Contain(labels);
        response.Content.Select(x => x.Value).Should().Contain(E2EConstants.EphemeralDialogUrn);
        response.Headers.ETagToGuid().Should().NotBeEmpty();
    }

    [E2EFact]
    public async Task Should_Create_Label()
    {
        // Arrange
        string[] labels = ["label1", "label2"];
        var dialogId = await CreateDialogWithLabels(labels);

        // Act
        var createResponse = await CreateServiceOwnerLabel(dialogId, "new-label");
        var getResponse = await GetServiceOwnerLabels(dialogId);

        // Assert
        createResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        getResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        getResponse.Content.Should().NotBeNull();
        getResponse.Content.Should().HaveCount(labels.Length + 2);
        getResponse.Content.Select(x => x.Value).Should().Contain(["label1", "label2", "new-label"]);
    }

    [E2EFact]
    public async Task Should_Reject_Duplicate_Label()
    {
        // Arrange
        const string label = "some-label";
        var dialogId = await CreateDialogWithLabels(label);

        // Act
        var response = await CreateServiceOwnerLabel(dialogId, label);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
        response.Error.Content.Should().Contain("duplicate");
    }

    [E2EFact]
    public async Task Should_Reject_Invalid_Label_Values()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var emptyResponse = await CreateServiceOwnerLabelWithoutValue(dialogId);
        var longResponse = await CreateServiceOwnerLabel(dialogId, new string('a', 300));
        var shortResponse = await CreateServiceOwnerLabel(dialogId, "a");

        // Assert
        emptyResponse.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        emptyResponse.Error.Should().NotBeNull();
        emptyResponse.Error.Content.Should().NotBeNull();
        emptyResponse.Error.Content.Should().Contain("not be empty");

        longResponse.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        longResponse.Error.Should().NotBeNull();
        longResponse.Error.Content.Should().NotBeNull();
        longResponse.Error.Content.Should().Contain("or fewer");

        shortResponse.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        shortResponse.Error.Should().NotBeNull();
        shortResponse.Error.Content.Should().NotBeNull();
        shortResponse.Error.Content.Should().Contain("at least");
    }

    [E2EFact]
    public async Task Should_Return_New_Revision_Header_When_Adding_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var initialResponse = await GetServiceOwnerLabels(dialogId);

        initialResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var initialRevision = initialResponse.Headers.ETagToGuid();

        // Act
        var response = await CreateServiceOwnerLabel(dialogId, "new-label");

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        response.Headers.ETagToGuid().Should().NotBe(initialRevision);
    }

    [E2EFact]
    public async Task Should_Return_404_When_Deleting_Missing_Label()
    {
        // Arrange
        const string missingLabel = "non-existent-label";
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await DeleteServiceOwnerLabel(dialogId, missingLabel);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
        response.Error.Content.Should().Contain("not found");
        response.Error.Content.Should().Contain(missingLabel);
    }

    [E2EFact]
    public async Task Should_Delete_Label()
    {
        // Arrange
        string[] labels = ["label1", "label2"];
        var dialogId = await CreateDialogWithLabels(labels);
        var labelToDelete = labels.First();

        // Act
        var deleteResponse = await DeleteServiceOwnerLabel(dialogId, labelToDelete);
        var getResponse = await GetServiceOwnerLabels(dialogId);

        // Assert
        deleteResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        getResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        getResponse.Content.Should().NotBeNull();
        getResponse.Content.Should().HaveCount(labels.Length);
        getResponse.Content.Select(x => x.Value).Should().NotContain(labelToDelete);
        getResponse.Content.Select(x => x.Value).Should().Contain(labels.Except([labelToDelete]));
    }

    private Task<Guid> CreateDialogWithLabels(params string[] labels) =>
        Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.ServiceOwnerContext = new()
            {
                ServiceOwnerLabels =
                [
                    ..labels.Select(label => new V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel
                    {
                        Value = label
                    })
                ]
            };
        });

    private Task<IApiResponse<ICollection<V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabels_ServiceOwnerLabel>>> GetServiceOwnerLabels(
        Guid dialogId) =>
        Fixture.ServiceownerApi.V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabelServiceOwnerLabel(
            dialogId,
            TestContext.Current.CancellationToken);

    private Task<IApiResponse> CreateServiceOwnerLabel(
        Guid dialogId,
        string label) =>
        Fixture.ServiceownerApi.V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabelServiceOwnerLabel(
            dialogId,
            new() { Value = label },
            null,
            TestContext.Current.CancellationToken);

    private Task<IApiResponse> CreateServiceOwnerLabelWithoutValue(
        Guid dialogId) =>
        Fixture.ServiceownerApi.V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabelServiceOwnerLabel(
            dialogId,
            new(),
            null,
            TestContext.Current.CancellationToken);

    private Task<IApiResponse> DeleteServiceOwnerLabel(
        Guid dialogId,
        string label) =>
        Fixture.ServiceownerApi.V1ServiceOwnerServiceOwnerContextCommandsDeleteServiceOwnerLabelServiceOwnerLabel(
            dialogId,
            label,
            null,
            TestContext.Current.CancellationToken);
}
