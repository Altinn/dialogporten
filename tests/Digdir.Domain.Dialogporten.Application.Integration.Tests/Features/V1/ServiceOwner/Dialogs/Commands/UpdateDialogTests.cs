using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
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
            .UpdateDialog(x =>
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
            .UpdateDialog(x =>
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
            .UpdateDialog(_ => { })
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
            .UpdateDialog(x =>
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
            .UpdateDialog(x =>
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
            .UpdateDialog(x =>
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
            .UpdateDialog(x =>
                x.Dto.Content!.Summary = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Content!.Summary.Should().BeNull());

    [Fact]
    public Task Cannot_Update_Content_To_Null_If_IsApiOnlyFalse_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .UpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Can_Update_Content_Summary_To_Null_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => x.Dto.Content!.Summary = null)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => { x.Dto.Content!.Title = null!; })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content.Title)));

    [Fact]
    public Task Can_Update_IsApiOnly_From_False_To_True() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .UpdateDialog(x => x.Dto.IsApiOnly = true)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IsApiOnly.Should().BeTrue());

    [Fact]
    public Task Can_Update_IsApiOnly_From_True_To_False() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => x.Dto.IsApiOnly = false)
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
            .UpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content)));

    [Fact]
    public Task Cannot_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddTransmission(x => x.Content = null);
                x.Dto.IsApiOnly = true;
            })
            .UpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<ValidationError>(x => x.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Transmissions)));

    [Fact]
    public Task Can_Update_IsApiOnly_To_False_If_Transmission_Content_Is_Not_Null() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddTransmission();
                x.Dto.IsApiOnly = true;
            })
            .UpdateDialog(x => x.Dto.IsApiOnly = false)
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_Attachment()
    {
        var userDefinedAttachmentId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
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
        updatedDialog!.Attachments
            .Should()
            .ContainSingle(x => x.Id == userDefinedAttachmentId);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_ApiAction()
    {
        var userDefinedApiActionId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
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
        updatedDialog!.ApiActions
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
            .UpdateDialog(x =>
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

    [Fact]
    public Task Adding_Transmission_Should_Increate_IncomingTransmissionsSO()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x => x.IncomingTransmissions.Should().Be(0))
            .UpdateDialog(x =>
                x.Dto.Transmissions.Add(new TransmissionDto
                {
                    Type = DialogTransmissionType.Values.Information,
                    Sender = new ActorDto
                    {
                        ActorType = ActorType.Values.ServiceOwner,
                    },
                })).GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.IncomingTransmissions.Should().Be(1));
    }
}
