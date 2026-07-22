using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.DialogEndUserContextsEntities_SystemLabel;
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
                    AddLabels = [Bin]
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

        dialog1.EndUserContext.SystemLabels.Should().Contain(Bin);
        dialog2.EndUserContext.SystemLabels.Should().Contain(Bin);
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
                request => request.AddLabels = [Bin],
                ifMatch: Guid.NewGuid());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.PreconditionFailed);
        var dialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        dialog.Should().NotBeNull();
        dialog.Content!.EndUserContext.SystemLabels.Should().ContainSingle().Which.Should().Be(Default);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unknown_Dialog()
    {
        // Act
        var setLabelResponse = await Fixture.ServiceownerApi
            .SetSystemLabel(
                Guid.CreateVersion7(),
                E2EConstants.DefaultParty,
                request => request.AddLabels = [Bin]
            );

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unauthorized_Dialog_When_Dialog_Has_Unauthorized_Party()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.Party = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{E2EConstants.GetDefaultServiceOwnerOrgNr()}");

        // Act
        var response = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [Archive]);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        var dialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        dialog.Should().NotBeNull();
        dialog.Content!.EndUserContext.SystemLabels.Should().ContainSingle().Which.Should().Be(Default);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unauthorized_Dialog_When_Trying_To_Modify_With_Another_Party()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        var setLabelResponse = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.AlternateParty,
                request => request.AddLabels = [Bin]
            );

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        var dialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        dialog.Should().NotBeNull();
        dialog.Content!.EndUserContext.SystemLabels.Should().ContainSingle().Which.Should().Be(Default);
    }

    [E2EFact]
    public async Task Should_Return_404_For_Unauthorized_Dialog_When_Token_Has_Unauthorized_Org()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        // Act
        Fixture.UseServiceOwnerTokenOverrides("964951284", "hko");
        var setLabelResponse = await Fixture.ServiceownerApi
            .SetSystemLabel(
                dialogId,
                E2EConstants.DefaultParty,
                request => request.AddLabels = [Bin]
            );
        Fixture.UseServiceOwnerTokenOverrides();

        // Assert
        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        var dialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        dialog.Should().NotBeNull();
        dialog.Content!.EndUserContext.SystemLabels.Should().ContainSingle().Which.Should().Be(Default);
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

    [E2ETheory]
    [ClassData(typeof(MultipleSystemLabelTestData))]
    public async Task Should_Apply_SystemLabel_Changes_As_Admin(MultipleSystemLabelScenario scenario)
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi
            .CreateSimpleDialogAsync(dialog =>
                dialog.SystemLabel = scenario.InitialLabel);

        // Act
        using var _ = Fixture.UseServiceOwnerTokenOverrides(
            scopes: E2EConstants.ServiceOwnerScopes + " " + AuthorizationScope.ServiceOwnerAdminScope
        );
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
                InitialLabel = Default,
                LabelsToAdd = [
                    Default,
                    Bin,
                    Archive],
                LabelsToRemove = [],
                ExpectedLabel = Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Default, Archive, Bin -> Bin",
                InitialLabel = Default,
                LabelsToAdd = [
                    Default,
                    Archive,
                    Bin],
                LabelsToRemove = [],
                ExpectedLabel = Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin, Default, Archive -> Archive",
                InitialLabel = Default,
                LabelsToAdd = [
                    Bin,
                    Default,
                    Archive],
                LabelsToRemove = [],
                ExpectedLabel = Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin, Archive, Default -> Default",
                InitialLabel = Default,
                LabelsToAdd = [
                    Bin,
                    Archive,
                    Default],
                LabelsToRemove = [],
                ExpectedLabel = Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive, Default, Bin -> Bin",
                InitialLabel = Default,
                LabelsToAdd = [
                    Archive,
                    Default,
                    Bin],
                LabelsToRemove = [],
                ExpectedLabel = Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive, Bin, Default -> Default",
                InitialLabel = Default,
                LabelsToAdd = [
                    Archive,
                    Bin,
                    Default],
                LabelsToRemove = [],
                ExpectedLabel = Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive + empty AddLabels -> Archive",
                InitialLabel = Archive,
                LabelsToAdd = [],
                LabelsToRemove = [],
                ExpectedLabel = Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive -> Default",
                InitialLabel = Archive,
                LabelsToAdd = [],
                LabelsToRemove = [Archive],
                ExpectedLabel = Default
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Bin -> Archive",
                InitialLabel = Archive,
                LabelsToAdd = [],
                LabelsToRemove = [Bin],
                ExpectedLabel = Archive
            });

            // RemoveLabels is evaluated before AddLabels
            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive + Bin -> Bin",
                InitialLabel = Archive,
                LabelsToAdd = [Bin],
                LabelsToRemove = [Archive],
                ExpectedLabel = Bin
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Bin - Bin + Archive -> Archive",
                InitialLabel = Bin,
                LabelsToAdd = [Archive],
                LabelsToRemove = [Bin],
                ExpectedLabel = Archive
            });

            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Archive - Archive + Archive -> Archive",
                InitialLabel = Archive,
                LabelsToAdd = [Archive],
                LabelsToRemove = [Archive],
                ExpectedLabel = Archive
            });


            Add(new MultipleSystemLabelScenario
            {
                DisplayName = "Default - Archive + Archive -> Archive",
                InitialLabel = Default,
                LabelsToAdd = [Archive],
                LabelsToRemove = [Archive],
                ExpectedLabel = Archive
            });
        }
    }
}
