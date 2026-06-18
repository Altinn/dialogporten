using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Patch;

[Collection(nameof(WebApiTestCollectionFixture))]
public class PatchDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public sealed record PatchDialogTestsScenario(
        string DisplayName,
        Action<V1ServiceOwnerDialogsCommandsCreate_Dialog> ModifyDialog,
        List<JsonPatchOperations_Operation> Patches,
        Action<V1ServiceOwnerDialogsQueriesGet_Dialog> AssertUpdate
    )
    {
        public override string ToString() => DisplayName;
    }

    private sealed class PatchDialogTestsData : TheoryData<PatchDialogTestsScenario>
    {
        public PatchDialogTestsData()
        {
            Add(new PatchDialogTestsScenario(
                "replace status: New -> InProgress",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "InProgress" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.InProgress); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> Draft",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "Draft" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.Draft); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> Sent (maps to Awaiting)",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "Sent" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.Awaiting); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> RequiresAttention",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [
                    new() { Op = "replace", Path = "/status", Value = "RequiresAttention" }
                ],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.RequiresAttention); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> Completed",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "Completed" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.Completed); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> NotApplicable",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "NotApplicable" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.NotApplicable); })
            );

            Add(new PatchDialogTestsScenario(
                "replace status: New -> Awaiting",
                modifyDialog => { modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New; },
                [new() { Op = "replace", Path = "/status", Value = "Awaiting" }],
                updated => { updated.Status.Should().Be(DialogsEntities_DialogStatus.Awaiting); })
            );

            Add(new PatchDialogTestsScenario(
                "replace progress",
                modifyDialog => { modifyDialog.Progress = 0; },
                [new() { Op = "replace", Path = "/progress", Value = 75 }],
                updated => { updated.Progress.Should().Be(75); })
            );

            Add(new PatchDialogTestsScenario(
                "replace extendedStatus",
                modifyDialog => { modifyDialog.ExtendedStatus = ""; },
                [new() { Op = "replace", Path = "/extendedStatus", Value = "urn:test:extended-status" }],
                updated => { updated.ExtendedStatus.Should().Be("urn:test:extended-status"); })
            );

            Add(new PatchDialogTestsScenario(
                "replace externalReference",
                modifyDialog => { modifyDialog.ExternalReference = ""; },
                [new() { Op = "replace", Path = "/externalReference", Value = "ext-ref-123" }],
                updated => { updated.ExternalReference.Should().Be("ext-ref-123"); })
            );

            Add(new PatchDialogTestsScenario(
                "replace dueAt",
                modifyDialog => { modifyDialog.DueAt = new DateTimeOffset(3233, 1, 1, 0, 0, 0, TimeSpan.Zero); },
                [new() { Op = "replace", Path = "/dueAt", Value = "2233-01-01T00:00:00Z" }],
                updated => { updated.DueAt.Should().Be(new DateTimeOffset(2233, 1, 1, 0, 0, 0, TimeSpan.Zero)); })
            );

            Add(new PatchDialogTestsScenario(
                "Remove dueAt",
                modifyDialog => { modifyDialog.DueAt = new DateTimeOffset(3233, 1, 1, 0, 0, 0, TimeSpan.Zero); },
                [new() { Op = "remove", Path = "/dueAt" }],
                updated => { updated.DueAt.Should().BeNull(); })
            );

            Add(new PatchDialogTestsScenario(
                "replace expiresAt",
                modifyDialog => { modifyDialog.ExpiresAt = new DateTimeOffset(3233, 1, 1, 0, 0, 0, TimeSpan.Zero); },
                [new() { Op = "replace", Path = "/expiresAt", Value = "4233-01-01T00:00:00Z" }],
                updated => { updated.ExpiresAt.Should().Be(new DateTimeOffset(4233, 1, 1, 0, 0, 0, TimeSpan.Zero)); })
            );

            Add(new PatchDialogTestsScenario(
                "replace process",
                modifyDialog => { modifyDialog.Process = ""; },
                [new() { Op = "replace", Path = "/process", Value = "urn:test:process:1" }],
                updated => { updated.Process.Should().Be("urn:test:process:1"); })
            );

            Add(new PatchDialogTestsScenario(
                "replace content title",
                modifyDialog => { modifyDialog.Content = CreateBasicContent(); },
                [
                    new()
                    {
                        Op = "replace",
                        Path = "/content/title",
                        Value = new { value = new[] { new { value = "Ny tittel", languageCode = "nb" } } }
                    }
                ],
                updated =>
                {
                    updated.Content.Title.Value.Should()
                        .ContainSingle(l => l.LanguageCode == "nb" && l.Value == "Ny tittel");
                })
            );

            Add(new PatchDialogTestsScenario(
                "replace content summary",
                modifyDialog => { modifyDialog.Content = CreateBasicContent(); },
                [
                    new()
                    {
                        Op = "replace",
                        Path = "/content/summary",
                        Value = new { value = new[] { new { value = "Nytt sammendrag", languageCode = "nb" } } }
                    }
                ],
                updated =>
                {
                    updated.Content.Summary.Value.Should()
                        .ContainSingle(l => l.LanguageCode == "nb" && l.Value == "Nytt sammendrag");
                })
            );

            Add(new PatchDialogTestsScenario(
                "replace content senderName",
                modifyDialog => { modifyDialog.Content = CreateBasicContent(); },
                [
                    new()
                    {
                        Op = "replace",
                        Path = "/content/senderName",
                        Value = new { value = new[] { new { value = "Ny avsender", languageCode = "nb" } } }
                    }
                ],
                updated =>
                {
                    updated.Content.SenderName.Value.Should()
                        .ContainSingle(l => l.LanguageCode == "nb" && l.Value == "Ny avsender");
                })
            );

            Add(new PatchDialogTestsScenario(
                "replace content additionalInfo",
                modifyDialog => { modifyDialog.Content = CreateBasicContent(); },
                [
                    new()
                    {
                        Op = "replace",
                        Path = "/content/additionalInfo",
                        Value = new { value = new[] { new { value = "Ny tilleggsinformasjon", languageCode = "nb" } } }
                    }
                ],
                updated =>
                {
                    updated.Content.AdditionalInfo.Value.Should()
                        .ContainSingle(l => l.LanguageCode == "nb" && l.Value == "Ny tilleggsinformasjon");
                })
            );

            Add(new PatchDialogTestsScenario(
                "Replace searchTags",
                modifyDialog =>
                {
                    modifyDialog.SearchTags =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_Tag
                        {
                            Value = "tag"
                        }
                    ];
                },
                [
                    new()
                    {
                        Op = "replace",
                        Path = "/searchTags",
                        Value = new[] { new { value = "søkeord-1" }, new { value = "søkeord-2" } }
                    }
                ],
                updated =>
                {
                    updated.SearchTags.Should().HaveCount(2);
                    updated.SearchTags.Should().Contain(t => t.Value == "søkeord-1");
                    updated.SearchTags.Should().Contain(t => t.Value == "søkeord-2");
                })
            );

            Add(new PatchDialogTestsScenario(
                "Multiple replace patches",
                modifyDialog =>
                {
                    modifyDialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.New;
                    modifyDialog.Progress = 10;
                },
                [
                    new() { Op = "replace", Path = "/status", Value = "InProgress" },
                    new() { Op = "replace", Path = "/progress", Value = 50 }
                ],
                updated =>
                {
                    updated.Status.Should().Be(DialogsEntities_DialogStatus.InProgress);
                    updated.Progress.Should().Be(50);
                })
            );
        }

        private static V1ServiceOwnerDialogsCommandsCreate_Content CreateBasicContent() => new()
        {
            Title = new V1CommonContent_ContentValue
            {
                Value =
                [
                    new V1CommonLocalizations_Localization
                    {
                        Value = "default",
                        LanguageCode = "nb"
                    }
                ],
                MediaType = "text/plain",
            },
        };
    }

    [E2ETheory]
    [ClassData(typeof(PatchDialogTestsData))]
    public async Task Patch_Dialog_Tests(PatchDialogTestsScenario scenario)
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(scenario.ModifyDialog);
        var createDialogRes = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(dialog);
        createDialogRes.Error?.Content.Should().BeNull();
        createDialogRes.ShouldHaveStatusCode(HttpStatusCode.Created);
        var dialogId = createDialogRes.Content.ToGuid();

        // Act
        var patchRes = await Fixture.ServiceownerApi.PatchDialogAsync(dialogId, o => o.AddRange(scenario.Patches));

        // Assert
        patchRes.Error?.Content.Should().BeNull();
        patchRes.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var updatedDialog = await Fixture.ServiceownerApi.GetDialog(dialogId);
        updatedDialog.ShouldHaveStatusCode(HttpStatusCode.OK);
        updatedDialog.Content.Should().NotBeNull();

        scenario.AssertUpdate(updatedDialog.Content);
    }
}
