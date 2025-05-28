using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using ApiActionDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ApiActionDto;
using AttachmentDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.AttachmentDto;
using GuiActionDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.GuiActionDto;
using TransmissionDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task UpdateDialogCommand_Should_Set_New_Revision_If_IsSilentUpdate_Is_Set()
    {
        // Arrange AND act
        Guid? revision = null!;

        var (updateSuccess, _) = await ArrangeAndAct(
            initialState: SimpleDialog,
            updateState: x =>
            {
                revision = x.IfMatchDialogRevision!.Value;
                x.IsSilentUpdate = true;
                x.Dto.Progress = (x.Dto.Progress % 100) + 1;
            },
            resultSelector: AssumeSuccess);

        // Assert
        updateSuccess.Revision.Should().NotBeEmpty();
        updateSuccess.Revision.Should().NotBe(revision!.Value);
    }


    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_SystemLabel_If_IsSilentUpdate_Is_Set()
    {
        // Arrange and Act
        var (_, updatedDialog) = await ArrangeAndAct(
            initialState: x => { x.Dto.SystemLabel = SystemLabel.Values.Bin; },
            updateState: x =>
            {
                x.IsSilentUpdate = true;
                x.Dto.SearchTags.Add(new() { Value = "crouching tiger, hidden update" });
            },
            resultSelector: AssumeSuccess);

        // Assert
        updatedDialog!.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_UpdatedAt_If_IsSilentUpdate_Is_Set()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Process = "updated:process";

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        updateResponse.TryPickT0(out _, out _).Should().BeTrue();

        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        getDialogDtoAfterUpdate.AsT0.UpdatedAt.Should().Be(getDialogDto.AsT0.UpdatedAt);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Return_New_Revision()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);
        var oldRevision = getDialogDto.AsT0.Revision;

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Update progress
        updateDialogDto.Progress = (updateDialogDto.Progress % 100) + 1;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(oldRevision);
    }

    [Fact]
    public async Task Cannot_Include_Old_Activities_To_UpdateCommand()
    {
        // Arrange
        var (_, createCommandResponse) = await GenerateDialogWithActivity();
        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Ref. old activity
        updateDialogDto.Activities.Add(new ActivityDto
        {
            Id = getDialogDto.AsT0.Activities.First().Id,
            Type = DialogActivityType.Values.DialogCreated,
            PerformedBy = new ActorDto
            {
                ActorType = ActorType.Values.ServiceOwner
            }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task Cannot_Include_Old_Transmissions_In_UpdateCommand()
    {
        // Arrange, Act, and Assert
        Guid? existingTransmissionId = null!;
        var domainError = await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
                existingTransmissionId = transmission.Id;
                x.Dto.Transmissions.Add(transmission);
            })
            .UpdateDialog(x =>
            {
                x.Dto.Transmissions.Add(new TransmissionDto
                {
                    Id = existingTransmissionId!.Value,
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
    public async Task Cannot_Update_Content_To_Null_If_IsApiOnlyFalse_Dialog()
    {
        // Arrange, Act, and Assert
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .UpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<ValidationError>();
    }

    [Fact]
    public async Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange, Act, and Assert
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => x.Dto.Content = null!)
            .ExecuteAndAssert<UpdateDialogSuccess>();
    }

    [Fact]
    public async Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange and Act
        var validationError = await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x => { x.Dto.Content!.Title = null!; })
            .ExecuteAndAssert<ValidationError>();

        // Assert
        validationError.ShouldHaveErrorWithText(nameof(UpdateDialogDto.Content.Title));
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_Attachment()
    {
        // Arrange and Act
        Guid? attachmentId = null!;

        var (_, updatedDialog) = await ArrangeAndAct(
            initialState: SimpleDialog,
            updateState: x =>
            {
                attachmentId = Guid.CreateVersion7();

                var attachment = new AttachmentDto
                {
                    Id = attachmentId.Value,
                    DisplayName = [new() { LanguageCode = "nb", Value = "Test attachment" }],
                    Urls =
                    [
                        new()
                        {
                            Url = new Uri("https://example.com"), ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        }
                    ]
                };
                x.Dto.Attachments.Add(attachment);
            },
            resultSelector: AssumeSuccess);

        // Assert
        updatedDialog!.Attachments
            .Should()
            .ContainSingle(x => x.Id == attachmentId!.Value);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_ApiAction()
    {
        // Arrange and Act
        Guid? apiActionId = null!;
        var (_, updatedDialog) = await ArrangeAndAct(
            initialState: SimpleDialog,
            updateState: x =>
            {
                apiActionId = Guid.CreateVersion7();

                var apiAction = new ApiActionDto
                {
                    Id = apiActionId.Value,
                    Action = "Test action",
                    Name = "Test action",
                    Endpoints = [new() { Url = new Uri("https://example.com"), HttpMethod = HttpVerb.Values.GET }]
                };
                x.Dto.ApiActions.Add(apiAction);
            },
            resultSelector: AssumeSuccess);

        // Assert
        updatedDialog!.ApiActions
            .Should()
            .ContainSingle(x => x.Id == apiActionId!.Value);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_GuiAction()
    {
        // Arrange
        var guiActionId = NewUuidV7();

        var updatedDialog = await FlowBuilder.For(Application)
            .CreateDialog(DialogGenerator.GenerateSimpleFakeCreateDialogCommand())
            .UpdateDialog(x =>
            {
                x.GuiActions.Add(new GuiActionDto
                {
                    Id = guiActionId,
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
            .ContainSingle(x => x.Id == guiActionId);
    }

    private async Task<(CreateDialogCommand, CreateDialogResult)> GenerateDialogWithActivity()
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.Information);
        activity.PerformedBy.ActorId = DialogGenerator.GenerateRandomParty(forcePerson: true);
        activity.PerformedBy.ActorName = null;
        createDialogCommand.Dto.Activities.Add(activity);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return (createDialogCommand, createCommandResponse);
    }

    // TODO: Static import for test project
    private static Guid NewUuidV7() => IdentifiableExtensions.CreateVersion7();
}

public static class ValidationErrorAssertionsExtensions
{
    public static void ShouldHaveErrorWithText(this ValidationError validationError, string expectedText)
    {
        validationError.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedText),
            $"Expected error containing the text '{expectedText}'");
    }
}

public static class DomainErrorAssertionsExtensions
{
    public static void ShouldHaveErrorWithText(this DomainError domainError, string expectedText)
    {
        domainError.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedText),
            $"Expected an error containing the text '{expectedText}'");
    }
}
