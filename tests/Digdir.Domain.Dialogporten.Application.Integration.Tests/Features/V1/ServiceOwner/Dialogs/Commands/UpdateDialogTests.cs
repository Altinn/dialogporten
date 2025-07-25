﻿using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using ApiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ApiActionDto;
using AttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.AttachmentDto;
using GuiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.GuiActionDto;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;
using DialogDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
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
                x.Dto.Progress = (x.Dto.Progress % 100) + 1;
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
            .CreateSimpleDialog(x => { x.Dto.SystemLabel = expectedSystemLabel; })
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
            .CreateSimpleDialog(x => x.Dto.UpdatedAt = x.Dto.CreatedAt = initialUpdatedAt)
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
            .CreateSimpleDialog(x =>
                x.Dto.UpdatedAt = x.Dto.CreatedAt =
                    initialDate)
            .AssertSuccessAndUpdateDialog(_ => { })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.UpdatedAt.Should()
                    .BeCloseTo(initialDate, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Return_New_Revision()
    {
        Guid? initialRevision = null!;

        var updateSuccess = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                initialRevision = x.IfMatchDialogRevision!.Value;
                x.Dto.Progress = (x.Dto.Progress % 100) + 1;
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
            .CreateSimpleDialog(x =>
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
            .CreateSimpleDialog(x =>
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
            .CreateSimpleDialog(x => x.Dto.Content!.Summary = new()
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
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Can_Update_Content_Summary_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.Content!.Summary = null)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => { x.Dto.Content!.Title = null!; })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content.Title)));

    [Fact]
    public Task Can_Update_IsApiOnly_From_False_To_True() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = true)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IsApiOnly.Should().BeTrue());

    [Fact]
    public Task Can_Update_IsApiOnly_From_True_To_False() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IsApiOnly.Should().BeFalse());

    [Fact]
    public Task Cannot_Update_IsApiOnly_To_False_If_Dialog_Content_Is_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.IsApiOnly = true;
                x.Dto.Content = null;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content)));

    [Fact]
    public Task Cannot_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddTransmission(x => x.Content = null);
                x.Dto.IsApiOnly = true;
            })
            .AssertSuccessAndUpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Transmissions)));

    [Fact]
    public Task Can_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Not_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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

    private sealed class ContentUpdatedAtTestData : TheoryData<string, Action<UpdateDialogCommand>>
    {
        public ContentUpdatedAtTestData()
        {
            const string baseDesc = "ContentUpdatedAt should update when";

            Add($"{baseDesc} dialog content is updated", x => x.ChangeTitle());
            Add($"{baseDesc} attachments are added", x => x.AddAttachment());
            Add($"{baseDesc} transmissions are added", x => x.AddTransmission());
            Add($"{baseDesc} GUI actions are added", x => x.AddGuiAction());
            Add($"{baseDesc} API actions are added", x => x.AddApiAction());
            Add($"{baseDesc} status changes", x => x.Dto.Status = DialogStatusInput.InProgress);
            Add($"{baseDesc} extended status changes", x => x.Dto.ExtendedStatus = "new extended status");
        }
    }

    [Theory, ClassData(typeof(ContentUpdatedAtTestData))]
    public Task ContentUpdatedAt_Should_Change_When_Content_Updates(string _,
        Action<UpdateDialogCommand> updateDialog) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Status = DialogStatusInput.Awaiting)
            .AssertSuccessAndUpdateDialog(updateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ContentUpdatedAt.Should().NotBe(x.CreatedAt);
                x.ContentUpdatedAt.Should().Be(x.UpdatedAt);
            });

    private sealed class ContentNotUpdatedAtTestData : TheoryData<string, Action<UpdateDialogCommand>>
    {
        public ContentNotUpdatedAtTestData()
        {
            const string baseDesc = "ContentUpdatedAt should not update when";

            Add($"{baseDesc} external referenced is updated", x => x.Dto.ExternalReference = "ext ref");
            Add($"{baseDesc} search tags are added", x => x.Dto.SearchTags.Add(new() { Value = "new tag" }));
            Add($"{baseDesc} process changes", x => x.Dto.Process = "some:process");
            Add($"{baseDesc} dueAt changes", x => x.Dto.DueAt = DateTimeOffset.UtcNow.AddYears(10));
            Add($"{baseDesc} expiresAt changes", x => x.Dto.ExpiresAt = DateTimeOffset.UtcNow.AddYears(10));
            Add($"{baseDesc} visibleFrom changes", x => x.Dto.VisibleFrom = x.Dto.DueAt!.Value.AddDays(-2));
            Add($"{baseDesc} progress changes", x => x.Dto.Progress = (x.Dto.Progress % 100) + 1);
            Add($"{baseDesc} isApiOnly changes", x => x.Dto.IsApiOnly = !x.Dto.IsApiOnly);
            Add($"{baseDesc} activities are added", x => x.AddActivity());
        }
    }

    [Theory, ClassData(typeof(ContentNotUpdatedAtTestData))]
    public Task ContentUpdatedAt_Should_Not_Change_When_Content_Not_Updated(string _,
        Action<UpdateDialogCommand> updateDialog) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(updateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ContentUpdatedAt.Should().Be(x.CreatedAt);
                x.ContentUpdatedAt.Should().NotBe(x.UpdatedAt);
            });

    private sealed class ContentNotUpdatedAtSilentUpdateTestData : TheoryData<string, Action<UpdateDialogCommand>>
    {
        public ContentNotUpdatedAtSilentUpdateTestData()
        {
            const string baseDesc = "ContentUpdatedAt should not update when";
            const string whenSilent = "and IsSilentUpdate is set to true";

            Add($"{baseDesc} dialog content is updated {whenSilent}", x => { x.ChangeTitle(); x.IsSilentUpdate = true; });
            Add($"{baseDesc} attachments are added {whenSilent}", x => { x.AddAttachment(); x.IsSilentUpdate = true; });
            Add($"{baseDesc} transmissions are added {whenSilent}", x => { x.AddTransmission(); x.IsSilentUpdate = true; });
            Add($"{baseDesc} GUI actions are added {whenSilent}", x => { x.AddGuiAction(); x.IsSilentUpdate = true; });
            Add($"{baseDesc} API actions are added {whenSilent}", x => { x.AddApiAction(); x.IsSilentUpdate = true; });
            Add($"{baseDesc} status changes {whenSilent}", x => { x.Dto.Status = DialogStatusInput.InProgress; x.IsSilentUpdate = true; });
            Add($"{baseDesc} extended status changes {whenSilent}", x => { x.Dto.ExtendedStatus = "new extended status"; x.IsSilentUpdate = true; });
        }
    }

    [Theory, ClassData(typeof(ContentNotUpdatedAtSilentUpdateTestData))]
    public Task ContentUpdatedAt_Should_Not_Change_When_Content_Updated_Silently(string _,
        Action<UpdateDialogCommand> updateDialog) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(updateDialog)
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
            .CreateSimpleDialog(x => x.AddTransmission(transmissionDto =>
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
}
