using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using ApiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ApiActionDto;
using AttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.AttachmentDto;
using GuiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.GuiActionDto;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;
using DialogDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchTagDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.SearchTagDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Update;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task UpdateDialogCommand_Should_Update_Correct_Dialog()
    {
        Guid? id = null;
        const string expectedExternalReference = "I've been updated!";
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = id = NewUuidV7();
                x.Dto.ExternalReference = "I'm so original...";
            })
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.Id = id!.Value;
                x.IfMatchDialogRevision = null;
                x.Dto.ExternalReference = expectedExternalReference;
            })
            .AssertSuccess()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .SendCommand(_ => new GetDialogQuery { DialogId = id!.Value })
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Id.Should().Be(id!.Value);
                x.ExternalReference.Should().Be(expectedExternalReference);
            });
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Set_New_Revision_If_IsSilentUpdate_Is_Set()
    {
        Guid? revision = null!;

        var updateSuccess = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                revision = x.IfMatchDialogRevision!.Value;
                x.IsSilentUpdate = true;
                x.Dto.Progress = 1;
            })
            .ExecuteAndAssert<UpdateDialogSuccess>();

        updateSuccess.Revision.Should().NotBeEmpty();
        updateSuccess.Revision.Should().NotBe(revision!.Value);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_SystemLabel_If_IsSilentUpdate_Is_Set()
    {
        var expectedSystemLabel = SystemLabel.Values.Bin;

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => { x.Dto.SystemLabel = expectedSystemLabel; })
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.IsSilentUpdate = true;
                x.Dto.SearchTags.Add(new() { Value = "crouching tiger, hidden update" });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        updatedDialog.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(expectedSystemLabel);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_UpdatedAt_If_IsSilentUpdate_Is_Set()
    {
        DateTimeOffset? initialUpdatedAt = null;

        await FlowBuilder.For(Application)
            // CreateAt must be set when UpdatedAt is set
            .CreateSimpleDialog((x, _) => x.Dto.UpdatedAt = x.Dto.CreatedAt = initialUpdatedAt)
            .AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => new GetDialogQuery { DialogId = ctx.GetDialogId() })
            .AssertResult<DialogDto>(x => initialUpdatedAt = x.UpdatedAt)
            .SendCommand(IFlowStepExtensions.CreateUpdateDialogCommand)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.UpdatedAt.Should().Be(initialUpdatedAt));
    }

    [Fact]
    public async Task Empty_Update_Should_Not_Set_UpdatedAt()
    {
        var initialDate = DateTimeOffset.UtcNow.AddYears(-1);
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.Dto.UpdatedAt = x.Dto.CreatedAt =
                    initialDate)
            .AssertSuccessAndUpdateDialog(_ => { })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.UpdatedAt.Should()
                    .BeCloseTo(initialDate, TimeSpan.FromSeconds(1)));
    }

    public sealed record DatesInPastScenario(string DisplayName, Action<UpdateDialogCommand> UpdateDialog) : ClassDataBase(DisplayName);

    private sealed class DatesInPastTestData : TheoryData<DatesInPastScenario>
    {
        public DatesInPastTestData()
        {
            var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

            Add(new DatesInPastScenario(
                DisplayName: "Can update dialog with DueAt in the past when IsSilentUpdate is set or admin scope is present",
                UpdateDialog: x => x.Dto.DueAt = pastDate));

            Add(new DatesInPastScenario(
                DisplayName: "Can update dialog with ExpiresAt in the past when IsSilentUpdate is set or admin scope is present",
                UpdateDialog: x => x.Dto.ExpiresAt = pastDate));
        }
    }

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Can_Update_Dialog_With_Past_Dates_When_Admin_Scope(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsAdminUser()
            .UpdateDialog(scenario.UpdateDialog)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Can_Update_Dialog_With_Past_Dates_When_Silent_Update(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.IsSilentUpdate = true;
                scenario.UpdateDialog(x);
            })
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Cannot_Update_Dialog_With_Past_Dates_Without_Silent_Update(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(scenario.UpdateDialog)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText("must be in future"));

    [Fact]
    public async Task UpdateDialogCommand_Should_Return_New_Revision()
    {
        Guid? initialRevision = null!;

        var updateSuccess = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                initialRevision = x.IfMatchDialogRevision!.Value;
                x.Dto.Progress = 1;
            })
            .ExecuteAndAssert<UpdateDialogSuccess>();

        updateSuccess.Revision.Should().NotBeEmpty();
        updateSuccess.Revision.Should().NotBe(initialRevision!.Value);
    }

    [Fact]
    public async Task Cannot_Include_Old_Activities_To_UpdateCommand()
    {
        var existingActivityId = NewUuidV7();

        var domainError = await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var activity =
                    DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.DialogCreated);
                activity.Id = existingActivityId;
                x.Dto.Activities.Add(activity);
            })
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Activities.Add(new ActivityDto
                {
                    Id = existingActivityId,
                    Type = DialogActivityType.Values.DialogCreated,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.ServiceOwner
                    }
                });
            })
            .ExecuteAndAssert<DomainError>();

        domainError.Errors
            .Should()
            .Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task Cannot_Include_Old_Transmissions_In_UpdateCommand()
    {
        var existingTransmissionId = NewUuidV7();

        var domainError = await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
                transmission.Id = existingTransmissionId;
                x.Dto.Transmissions.Add(transmission);
            })
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Transmissions.Add(new TransmissionDto
                {
                    Id = existingTransmissionId,
                    Type = DialogTransmissionType.Values.Information,
                    Sender = new() { ActorType = ActorType.Values.ServiceOwner },
                    Content = new()
                    {
                        Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                        Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
                    }
                });
            })
            .ExecuteAndAssert<DomainError>();

        domainError.ShouldHaveErrorWithText("already exists");
    }

    [Fact]
    public Task Can_Set_Dialog_Summary_To_Null_On_Update() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Content!.Summary = new()
            {
                Value = DialogGenerator.GenerateFakeLocalizations(1)
            })
            .AssertSuccessAndUpdateDialog(x =>
                x.Dto.Content!.Summary = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Content!.Summary.Should().BeNull());

    [Fact]
    public Task Cannot_Update_Content_To_Null_If_IsApiOnlyFalse_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = false)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content = null)
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content = null)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Can_Update_Content_Summary_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content!.Summary = null)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => { x.Dto.Content!.Title = null!; })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content.Title)));

    [Fact]
    public Task Can_Update_IsApiOnly_From_False_To_True() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = false)
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = true)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IsApiOnly.Should().BeTrue());

    [Fact]
    public Task Can_Update_IsApiOnly_From_True_To_False() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IsApiOnly.Should().BeFalse());

    [Fact]
    public Task Cannot_Update_IsApiOnly_To_False_If_Dialog_Content_Is_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.IsApiOnly = true;
                x.Dto.Content = null;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content)));

    [Fact]
    public Task Cannot_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddTransmission(x => x.Content = null);
                x.Dto.IsApiOnly = true;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Transmissions)));

    [Fact]
    public Task Can_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Not_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddTransmission();
                x.Dto.IsApiOnly = true;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_Attachment()
    {
        var userDefinedAttachmentId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Attachments.Add(new AttachmentDto
                {
                    Id = userDefinedAttachmentId,
                    DisplayName = [new() { LanguageCode = "nb", Value = "Test attachment" }],
                    Urls =
                    [
                        new()
                        {
                            Url = new Uri("https://example.com"), ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        }
                    ]
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        // Assert
        updatedDialog.Attachments
            .Should()
            .ContainSingle(x => x.Id == userDefinedAttachmentId);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_ApiAction()
    {
        var userDefinedApiActionId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.ApiActions.Add(new ApiActionDto
                {
                    Id = userDefinedApiActionId,
                    Action = "Test action",
                    Name = "Test action",
                    Endpoints = [new() { Url = new Uri("https://example.com"), HttpMethod = HttpVerb.Values.GET }]
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        // Assert
        updatedDialog.ApiActions
            .Should()
            .ContainSingle(x => x.Id == userDefinedApiActionId);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_GuiAction()
    {
        // Arrange
        var userDefinedGuiActionId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.GuiActions.Add(new GuiActionDto
                {
                    Id = userDefinedGuiActionId,
                    Action = "Test action",
                    Title = [new() { LanguageCode = "nb", Value = "Test action" }],
                    Priority = DialogGuiActionPriority.Values.Tertiary,
                    Url = new Uri("https://example.com"),
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        // Assert
        updatedDialog.GuiActions
            .Should()
            .ContainSingle(x => x.Id == userDefinedGuiActionId);
    }

    public sealed record ContentUpdatedAtScenario(
        string DisplayName,
        Action<UpdateDialogCommand> UpdateDialog) : ClassDataBase(DisplayName);

    private sealed class ContentUpdatedAtTestData : TheoryData<ContentUpdatedAtScenario>
    {
        public ContentUpdatedAtTestData()
        {
            const string baseDesc = "ContentUpdatedAt should update when";

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} dialog content is updated",
                UpdateDialog: x => x.ChangeTitle()));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} attachments are added",
                UpdateDialog: x => x.AddAttachment()));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} transmissions are added",
                UpdateDialog: x => x.AddTransmission()));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} GUI actions are added",
                UpdateDialog: x => x.AddGuiAction()));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} API actions are added",
                UpdateDialog: x => x.AddApiAction()));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} status changes",
                UpdateDialog: x => x.Dto.Status = DialogStatusInput.InProgress));

            Add(new ContentUpdatedAtScenario(
                DisplayName: $"{baseDesc} extended status changes",
                UpdateDialog: x => x.Dto.ExtendedStatus = "new extended status"));
        }
    }

    [Theory, ClassData(typeof(ContentUpdatedAtTestData))]
    public Task ContentUpdatedAt_Should_Change_When_Content_Updates(ContentUpdatedAtScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Status = DialogStatusInput.Awaiting)
            .AssertSuccessAndUpdateDialog(scenario.UpdateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ContentUpdatedAt.Should().NotBe(x.CreatedAt);
                x.ContentUpdatedAt.Should().Be(x.UpdatedAt);
            });

    public sealed record ContentNotUpdatedAtScenario(
        string DisplayName,
        Action<UpdateDialogCommand> UpdateDialog) : ClassDataBase(DisplayName);

    private sealed class ContentNotUpdatedAtTestData : TheoryData<ContentNotUpdatedAtScenario>
    {
        public ContentNotUpdatedAtTestData()
        {
            const string baseDesc = "ContentUpdatedAt should not update when";

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} external reference is updated",
                UpdateDialog: x => x.Dto.ExternalReference = "ext ref"));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} search tags are added",
                UpdateDialog: x => x.Dto.SearchTags.Add(new SearchTagDto { Value = "new tag" })));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} process changes",
                UpdateDialog: x => x.Dto.Process = "some:process"));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} dueAt changes",
                UpdateDialog: x => x.Dto.DueAt = DateTimeOffset.UtcNow.AddYears(10)));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} expiresAt changes",
                UpdateDialog: x => x.Dto.ExpiresAt = DateTimeOffset.UtcNow.AddYears(10)));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} progress changes",
                UpdateDialog: x => x.Dto.Progress = (x.Dto.Progress % 100) + 1));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} isApiOnly changes",
                UpdateDialog: x => x.Dto.IsApiOnly = !x.Dto.IsApiOnly));

            Add(new ContentNotUpdatedAtScenario(
                DisplayName: $"{baseDesc} activities are added",
                UpdateDialog: x => x.AddActivity()));
        }
    }

    [Theory, ClassData(typeof(ContentNotUpdatedAtTestData))]
    public Task ContentUpdatedAt_Should_Not_Change_When_Content_Not_Updated(ContentNotUpdatedAtScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Progress = 1)
            .AssertSuccessAndUpdateDialog(scenario.UpdateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ContentUpdatedAt.Should().Be(x.CreatedAt);
                x.ContentUpdatedAt.Should().NotBe(x.UpdatedAt);
            });

    [Fact]
    public Task Future_VisibleFrom_Should_Control_Timestamps_On_Update()
    {
        var visibleFrom = DateTimeOffset.UtcNow.AddDays(7);

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.VisibleFrom = visibleFrom;
                x.Dto.Progress = 36;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.Progress = 37)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
            {
                dialog.VisibleFrom.Should().BeCloseTo(visibleFrom, TimeSpan.FromSeconds(1));
                dialog.CreatedAt.Should().Be(dialog.VisibleFrom);
                dialog.UpdatedAt.Should().Be(dialog.VisibleFrom);
                dialog.ContentUpdatedAt.Should().Be(dialog.VisibleFrom);
            });
    }

    [Fact]
    public Task Cannot_Set_DueAt_Before_Saved_VisibleFrom()
    {
        var visibleFrom = DateTimeOffset.UtcNow.AddDays(10);

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.VisibleFrom = visibleFrom;
                x.Dto.DueAt = visibleFrom.AddDays(1);
                x.Dto.ExpiresAt = visibleFrom.AddDays(2);
            })
            .UpdateDialog(x => x.Dto.DueAt = visibleFrom.AddDays(-1))
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(UpdateDialogDto.DueAt)));
    }

    [Fact]
    public Task Cannot_Set_ExpiresAt_Before_Saved_VisibleFrom()
    {
        var visibleFrom = DateTimeOffset.UtcNow.AddDays(5);

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.VisibleFrom = visibleFrom;
                x.Dto.DueAt = visibleFrom.AddDays(1);
                x.Dto.ExpiresAt = visibleFrom.AddDays(2);
            })
            .UpdateDialog(x => x.Dto.ExpiresAt = visibleFrom.AddDays(-1))
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(UpdateDialogDto.ExpiresAt)));
    }

    public sealed record ContentNotUpdatedAtSilentUpdateScenario(
        string DisplayName,
        Action<UpdateDialogCommand> UpdateDialog) : ClassDataBase(DisplayName);

    private sealed class ContentNotUpdatedAtSilentUpdateTestData : TheoryData<ContentNotUpdatedAtSilentUpdateScenario>
    {
        public ContentNotUpdatedAtSilentUpdateTestData()
        {
            const string baseDesc = "ContentUpdatedAt should not update when";
            const string whenSilent = "and IsSilentUpdate is set to true";

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} dialog content is updated {whenSilent}",
                UpdateDialog: x => { x.ChangeTitle(); x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} attachments are added {whenSilent}",
                UpdateDialog: x => { x.AddAttachment(); x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} transmissions are added {whenSilent}",
                UpdateDialog: x => { x.AddTransmission(); x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} GUI actions are added {whenSilent}",
                UpdateDialog: x => { x.AddGuiAction(); x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} API actions are added {whenSilent}",
                UpdateDialog: x => { x.AddApiAction(); x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} status changes {whenSilent}",
                UpdateDialog: x => { x.Dto.Status = DialogStatusInput.InProgress; x.IsSilentUpdate = true; }));

            Add(new ContentNotUpdatedAtSilentUpdateScenario(
                DisplayName: $"{baseDesc} extended status changes {whenSilent}",
                UpdateDialog: x => { x.Dto.ExtendedStatus = "new extended status"; x.IsSilentUpdate = true; }));
        }
    }

    [Theory, ClassData(typeof(ContentNotUpdatedAtSilentUpdateTestData))]
    public Task ContentUpdatedAt_Should_Not_Change_When_Content_Updated_Silently(
        ContentNotUpdatedAtSilentUpdateScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(scenario.UpdateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.UpdatedAt.Should().Be(x.CreatedAt);
                x.ContentUpdatedAt.Should().Be(x.CreatedAt);
                x.ContentUpdatedAt.Should().Be(x.UpdatedAt);
            });

    [Fact]
    public async Task Dialog_Opened_Content()
    {
        var transmissionId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddTransmission(transmissionDto =>
            {
                transmissionDto.Id = transmissionId;
                transmissionDto.Type = DialogTransmissionType.Values.Information;
            }))
            .AssertSuccessAndUpdateDialog(x => x.Dto.Activities =
            [
                new ActivityDto
                {
                    Type = DialogActivityType.Values.TransmissionOpened,
                    TransmissionId = transmissionId,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.ServiceOwner
                    },
                }
            ])
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.HasUnopenedContent.Should().BeFalse();
                x.Transmissions.First().IsOpened.Should().BeTrue();
            });
    }

    [Fact]
    public Task Adding_Transmission_From_Party_Should_Increase_FromPartyTransmissionsCount()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x =>
            {
                x.FromServiceOwnerTransmissionsCount.Should().Be(0);
                x.FromPartyTransmissionsCount.Should().Be(0);
            })
            .UpdateDialog(x => x
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Correction))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.FromServiceOwnerTransmissionsCount.Should().Be(0);
                x.FromPartyTransmissionsCount.Should().Be(1);
            });
    }

    [Fact]
    public Task Adding_Transmission_From_ServiceOwner_Should_Increase_FromServiceOwnerTransmissionsCount()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>(x =>
            {
                x.FromServiceOwnerTransmissionsCount.Should().Be(0);
                x.FromPartyTransmissionsCount.Should().Be(0);
            })
            .UpdateDialog(x => x
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Information))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.FromServiceOwnerTransmissionsCount.Should().Be(1);
                x.FromPartyTransmissionsCount.Should().Be(0);
            });
    }

    [Theory, ClassData(typeof(AddingEndUserTransmissionSentLabelTestData))]
    public Task Adding_EndUser_Transmission_Adds_Sent_Label_If_Submission_Or_Correction(
        AddingEndUserTransmissionSentLabelScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x => x.Type = scenario.TransmissionType))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                if (scenario.ShouldAddSentLabel)
                {
                    x.EndUserContext.SystemLabels.Should().ContainSingle(
                        label => label == SystemLabel.Values.Sent);
                }
                else
                {
                    x.EndUserContext.SystemLabels.Should().NotContain(
                        label => label == SystemLabel.Values.Sent);
                }
            });

    [Theory, ClassData(typeof(DialogContentLengthTestData))]
    public Task Content_Length_Validation_Test(
        DialogContentLengthScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(scenario.UpdateDialog)
            .ExecuteAndAssert(scenario.ExpectedResultType);

    public sealed record DialogContentLengthScenario(
        string DisplayName,
        Action<UpdateDialogCommand> UpdateDialog,
        Type ExpectedResultType) : ClassDataBase(DisplayName);

    private sealed class DialogContentLengthTestData : TheoryData<DialogContentLengthScenario>
    {
        private static string Repeat(char c, int x) => new(c, x);
        private static int GetMaxLength(DialogContentType.Values value) =>
            DialogContentType.GetValue(value).MaxLength;

        public DialogContentLengthTestData()
        {
            AddLengthTests((x, value) => x.Dto.Content!.Title = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.Title));

            AddLengthTests((x, value) => x.Dto.Content!.SenderName = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.SenderName));

            AddLengthTests((x, value) => x.Dto.Content!.Summary = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.Summary));

            AddLengthTests((x, value) => x.Dto.Content!.AdditionalInfo = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.AdditionalInfo));

            AddLengthTests((x, value) => x.Dto.Content!.ExtendedStatus = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.ExtendedStatus));

            AddLengthTests((x, value) => x.Dto.Content!.NonSensitiveTitle = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.NonSensitiveTitle));

            AddLengthTests((x, value) => x.Dto.Content!.NonSensitiveSummary = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.NonSensitiveSummary));
        }

        private void AddLengthTests(Action<UpdateDialogCommand, string> applyValue, int maxLength)
        {
            Add(new DialogContentLengthScenario(
                DisplayName: $"Dialog content length {maxLength} characters",
                UpdateDialog: x => applyValue(x, Repeat('x', maxLength)),
                ExpectedResultType: typeof(UpdateDialogSuccess)));

            Add(new DialogContentLengthScenario(
                DisplayName: $"Dialog content length {maxLength + 1} characters",
                UpdateDialog: x => applyValue(x, Repeat('x', maxLength + 1)),
                ExpectedResultType: typeof(ValidationError)));
        }

        private static ContentValueDto CreateContentDto(string content) => new()
        {
            Value = [new() { Value = content, LanguageCode = "nb" }]
        };
    }
}
