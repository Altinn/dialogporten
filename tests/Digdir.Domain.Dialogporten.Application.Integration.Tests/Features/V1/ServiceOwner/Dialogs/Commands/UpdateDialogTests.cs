using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
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
using MediatR;
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
            initialState: x =>
            {
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
            },
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
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var existingTransmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
        createDialogCommand.Dto.Transmissions.Add(existingTransmission);
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Ref. old transmission
        updateDialogDto.Transmissions.Add(new TransmissionDto
        {
            Id = existingTransmission.Id,
            Type = DialogTransmissionType.Values.Information,
            Sender = new() { ActorType = ActorType.Values.ServiceOwner },
            Content = new()
            {
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
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
    public async Task Cannot_Update_Content_To_Null_If_IsApiOnlyFalse_Dialog()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Content = null!;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.IsApiOnly = true;
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Content = null!;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange and Act
        var (validationError, _) = await ArrangeAndAct(
            initialState: dialog =>
            {
                dialog.Dto.IsApiOnly = true;
            },
            updateState: x =>
            {
                x.Dto.Content!.Title = null!; // Content is supplied, but title is not (only summary)
            },
            resultSelector: AssumeBadRequest);

        // Assert
        validationError.Errors
            .Should()
            .ContainSingle(e => e
                .ErrorMessage
                .Contains(nameof(UpdateDialogDto.Content.Title)));

        // // Arrange
        // var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        // createDialogCommand.Dto.IsApiOnly = true;
        // var createCommandResponse = await Application.Send(createDialogCommand);
        //
        // var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        // var getDialogDto = await Application.Send(getDialogQuery);
        //
        // var mapper = Application.GetMapper();
        // var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        // updateDialogDto.Content!.Title = null!; // Content is supplied, but title is not (only summary)
        //
        // // Act
        // var updateResponse = await Application.Send(new UpdateDialogCommand
        // {
        //     Id = createCommandResponse.AsT0.DialogId,
        //     Dto = updateDialogDto
        // });
        //
        // // Assert
        // updateResponse.TryPickT3(out var validationError, out _).Should().BeTrue();
        // validationError.Should().NotBeNull();
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
                    Urls = [new() { Url = new Uri("https://example.com"), ConsumerType = AttachmentUrlConsumerType.Values.Gui }]
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
        // // Arrange
        // var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        // var createCommandResponse = await Application.Send(createDialogCommand);
        //
        // var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        // var getDialogDto = await Application.Send(getDialogQuery);
        //
        // var mapper = Application.GetMapper();
        // var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        //
        // var apiAction = new ApiActionDto
        // {
        //     Id = Guid.CreateVersion7(),
        //     Action = "Test action",
        //     Name = "Test action",
        //     Endpoints = [new() { Url = new("https://example.com"), HttpMethod = HttpVerb.Values.GET }]
        // };
        // updateDialogDto.ApiActions.Add(apiAction);
        //
        // // Act
        // var updateResponse = await Application.Send(new UpdateDialogCommand
        // {
        //     Id = createCommandResponse.AsT0.DialogId,
        //     Dto = updateDialogDto
        // });
        // var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);
        //
        // // Assert
        // updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        // success.Should().NotBeNull();
        // getDialogDtoAfterUpdate.TryPickT0(out var currentDialog, out _).Should().BeTrue();
        // currentDialog.ApiActions.Should().Contain(x => x.Id == apiAction.Id);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_GuiAction()
    {
        // Arrange
        var guiActionId = IdentifiableExtensions.CreateVersion7();
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var applicationFlow = await new ApplicationFlowBuilder(Application)
            .SendCommand(DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId))
            .SendCommand(x => new GetDialogQuery { DialogId = dialogId })
            .Select(x => Application.GetMapper().Map<UpdateDialogDto>(x.AsT0))
            .Select(x =>
            {
                x.GuiActions.Add(new GuiActionDto
                {
                    Id = guiActionId,
                    Action = "Test action",
                    Title = [new() { LanguageCode = "nb", Value = "Test action" }],
                    Priority = DialogGuiActionPriority.Values.Tertiary,
                    Url = new Uri("https://example.com"),
                });
                return x;
            })
            .SendCommand(x => new UpdateDialogCommand { Id = dialogId, Dto = x })
            .SendCommand(x => new GetDialogQuery { DialogId = dialogId })
            .ExecuteAsync();

        var applicationFlow = await new ApplicationFlowBuilder()
            .CreateDialog(DialogGenerator.GenerateSimpleFakeCreateDialogCommand())
            // .GetDialog(x => x.AsT0.DialogId)
            // .Select(x => Application.GetMapper().Map<UpdateDialogDto>(x.AsT0))
            // .Select(x =>
            // {
            //     x.GuiActions.Add(new GuiActionDto
            //     {
            //         Id = guiActionId,
            //         Action = "Test action",
            //         Title = [new() { LanguageCode = "nb", Value = "Test action" }],
            //         Priority = DialogGuiActionPriority.Values.Tertiary,
            //         Url = new Uri("https://example.com"),
            //     });
            //     return x;
            // })
            // .SendCommand(x => new UpdateDialogCommand { Id = x.Id, Dto = x })
            .UpdateDialog(x => x.GuiActions.Add(new GuiActionDto
            {
                Id = guiActionId,
                Action = "Test action",
                Title = [new() { LanguageCode = "nb", Value = "Test action" }],
                Priority = DialogGuiActionPriority.Values.Tertiary,
                Url = new Uri("https://example.com"),
            }))
            .SendCommand(x => new GetDialogQuery { DialogId = x.AsT0.DialogId })
            .ExecuteAsync();

        var result = await applicationFlow.ExecuteAsync();


        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        var guiAction = new GuiActionDto
        {
            Id = Guid.CreateVersion7(),
            Action = "Test action",
            Title = [new() { LanguageCode = "nb", Value = "Test action" }],
            Priority = DialogGuiActionPriority.Values.Tertiary,
            Url = new Uri("https://example.com"),
        };
        updateDialogDto.GuiActions.Add(guiAction);

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });
        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        getDialogDtoAfterUpdate.TryPickT0(out var currentDialog, out _).Should().BeTrue();
        currentDialog.GuiActions.Should().Contain(x => x.Id == guiAction.Id);
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

    private static ValidationError AssumeBadRequest(UpdateDialogResult updateDialogResult)
    {
        updateDialogResult.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        return validationError;
    }

    private static UpdateDialogSuccess AssumeSuccess(UpdateDialogResult updateDialogResult)
    {
        updateDialogResult.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        return success;
    }

    private static void SimpleDialog(CreateDialogCommand x) { }

    private static CreateDialogCommand ComplexDialog(CreateDialogCommand _) =>
        DialogGenerator.GenerateFakeCreateDialogCommand();

    private static UpdateDialogResult? DefaultResultSelector(UpdateDialogResult? x) => x;

    private async Task<(T Result, DialogDto? UpdatedDialog)> ArrangeAndAct<T>(
        Action<CreateDialogCommand> initialState,
        Action<UpdateDialogCommand> updateState,
        Func<UpdateDialogResult, T> resultSelector)
    {
        // resultSelector ??= DefaultResultSelector;

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        initialState(createDialogCommand);

        var dialogId = await Application.CreateDialog(createDialogCommand);
        var dialog = await Application.GetDialog(dialogId);

        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dialogId,
            Dto = Application.GetMapper().Map<UpdateDialogDto>(dialog),
            IfMatchDialogRevision = dialog.Revision
        };

        updateState(updateDialogCommand);

        var updateDialogResponse = await Application.Send(updateDialogCommand);
        var result = resultSelector(updateDialogResponse);

        DialogDto? updatedDialog = null;
        if (updateDialogResponse.IsT0)
        {
            updatedDialog = await Application.GetDialog(dialogId);
        }

        return (result, updatedDialog);
    }
}

public static class ApplicationExtensions
{
    internal static async Task<Guid> CreateDialog(this DialogApplication application, CreateDialogCommand command)
        => (await application.Send(command)).AsT0.DialogId;

    internal static async Task<DialogDto> GetDialog(this DialogApplication application, Guid dialogId)
        => (await application.Send(new GetDialogQuery { DialogId = dialogId })).AsT0;
}

public class ApplicationFlowBuilder : IApplicationFlowStep
{
    private readonly DialogApplication _application;
    private readonly List<Step> _commands = [];

    public ApplicationFlowBuilder(DialogApplication application)
    {
        _application = application;
    }

    public IApplicationFlowStep<TOut> SendCommand<TOut>(IRequest<TOut> command)
    {
        throw new NotImplementedException();
    }

    private sealed class ApplicationFlowStep<TIn> : IApplicationFlowStep<TIn>
    {
        public IApplicationFlowStep<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector) => throw new NotImplementedException();

        public IApplicationFlowStep<TOut> Select<TOut>(Func<TIn, TOut> selector) => throw new NotImplementedException();

        public Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default)
        {

        }
    }
}

public interface IApplicationFlowStep
{
    IApplicationFlowStep<TOut> SendCommand<TOut>(IRequest<TOut> command);
}

public interface IApplicationFlowStep<TIn>
{
    IApplicationFlowStep<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector);
    IApplicationFlowStep<TOut> Select<TOut>(Func<TIn, TOut> selector);
    Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default);
}
