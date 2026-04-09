using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using ServiceOwnerSystemLabel = Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SetSystemLabelTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_BulkSet_Labels_For_Accessible_Dialogs()
    {
        // Arrange
        var dialogId1 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();
        var dialogId2 = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabelsBulkSetDialogSystemLabels(
                E2EConstants.DefaultParty,
                new V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel
                {
                    Dialogs =
                    [
                        new() { DialogId = dialogId1 },
                        new() { DialogId = dialogId2 }
                    ],
                    AddLabels = [ServiceOwnerSystemLabel.Bin]
                },
                TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var dialog1Response = await Fixture.ServiceownerApi.GetDialog(dialogId1, E2EConstants.DefaultParty);
        var dialog2Response = await Fixture.ServiceownerApi.GetDialog(dialogId2, E2EConstants.DefaultParty);

        dialog1Response.ShouldHaveStatusCode(HttpStatusCode.OK);
        dialog2Response.ShouldHaveStatusCode(HttpStatusCode.OK);

        var dialog1 = dialog1Response.Content ?? throw new InvalidOperationException("Dialog content was null.");
        var dialog2 = dialog2Response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        dialog1.EndUserContext.SystemLabels.Should().Contain(ServiceOwnerSystemLabel.Bin);
        dialog2.EndUserContext.SystemLabels.Should().Contain(ServiceOwnerSystemLabel.Bin);
    }

    [E2EFact]
    public async Task Should_Return_412_For_Invalid_IfMatch_Revision()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Bin],
                ifMatch: Guid.NewGuid());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unauthorized_Dialog()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{E2EConstants.GetDefaultServiceOwnerOrgNr()}");

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [ServiceOwnerSystemLabel.Archive]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2ETheory]
    [ClassData(typeof(MultipleSystemLabelTestData))]
    public async Task Should_Apply_SystemLabel_Changes(MultipleSystemLabelScenario scenario)
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(dialog =>
                dialog.SystemLabel = scenario.InitialLabel);

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request =>
                {
                    request.AddLabels = scenario.LabelsToAdd;
                    request.RemoveLabels = scenario.LabelsToRemove;
                });

        var dialogResponse = await Fixture.ServiceownerApi
            .GetDialog(dialogId, E2EConstants.DefaultParty);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        // The last label is selected when multiple of Default/Bin/Archive is supplied.
        // Removing Archive or Bin resets the label to Default unless another one is added.
        dialogResponse.Content!.EndUserContext.SystemLabels
            .Should().ContainSingle().Which
            .Should().Be(scenario.ExpectedLabel);
    }

    public sealed class MultipleSystemLabelScenario
    {
        public required string DisplayName { get; init; }
        public required ServiceOwnerSystemLabel InitialLabel { get; init; }
        public required ServiceOwnerSystemLabel[] LabelsToAdd { get; init; }
        public required ServiceOwnerSystemLabel[] LabelsToRemove { get; init; }
        public required ServiceOwnerSystemLabel ExpectedLabel { get; init; }

        public override string ToString() => DisplayName;
    }

    private sealed class MultipleSystemLabelTestData : TheoryData<MultipleSystemLabelScenario>
    {
        public MultipleSystemLabelTestData()
        {
            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Default, Bin, Archive -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Default,
                    ServiceOwnerSystemLabel.Bin,
                    ServiceOwnerSystemLabel.Archive],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Default, Archive, Bin -> Bin",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Default,
                    ServiceOwnerSystemLabel.Archive,
                    ServiceOwnerSystemLabel.Bin],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin, Default, Archive -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Bin,
                    ServiceOwnerSystemLabel.Default,
                    ServiceOwnerSystemLabel.Archive],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin, Archive, Default -> Default",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Bin,
                    ServiceOwnerSystemLabel.Archive,
                    ServiceOwnerSystemLabel.Default],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive, Default, Bin -> Bin",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Archive,
                    ServiceOwnerSystemLabel.Default,
                    ServiceOwnerSystemLabel.Bin],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive, Bin, Default -> Default",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [
                    ServiceOwnerSystemLabel.Archive,
                    ServiceOwnerSystemLabel.Bin,
                    ServiceOwnerSystemLabel.Default],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive + empty AddLabels -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Archive,
                LabelsToAdd = [],
                LabelsToRemove = [],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive -> Default",
                InitialLabel = ServiceOwnerSystemLabel.Archive,
                LabelsToAdd = [],
                LabelsToRemove = [ServiceOwnerSystemLabel.Archive],
                ExpectedLabel = ServiceOwnerSystemLabel.Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Bin -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Archive,
                LabelsToAdd = [],
                LabelsToRemove = [ServiceOwnerSystemLabel.Bin],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });

            // RemoveLabels is evaluated before AddLabels
            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive + Bin -> Bin",
                InitialLabel = ServiceOwnerSystemLabel.Archive,
                LabelsToAdd = [ServiceOwnerSystemLabel.Bin],
                LabelsToRemove = [ServiceOwnerSystemLabel.Archive],
                ExpectedLabel = ServiceOwnerSystemLabel.Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin - Bin + Archive -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Bin,
                LabelsToAdd = [ServiceOwnerSystemLabel.Archive],
                LabelsToRemove = [ServiceOwnerSystemLabel.Bin],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive + Archive -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Archive,
                LabelsToAdd = [ServiceOwnerSystemLabel.Archive],
                LabelsToRemove = [ServiceOwnerSystemLabel.Archive],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });


            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Default - Archive + Archive -> Archive",
                InitialLabel = ServiceOwnerSystemLabel.Default,
                LabelsToAdd = [ServiceOwnerSystemLabel.Archive],
                LabelsToRemove = [ServiceOwnerSystemLabel.Archive],
                ExpectedLabel = ServiceOwnerSystemLabel.Archive
            });
        }
    }
}
