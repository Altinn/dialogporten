using System.Reflection;
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
            initialState: dialog => { dialog.Dto.IsApiOnly = true; },
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
            .CreateSimpleDialog()
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
            .GetDialog(Guid.Empty)
            .AssertSuccess();

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

    private static void SimpleDialog(CreateDialogCommand x)
    {
    }

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

    private static Guid NewUuidV7() => IdentifiableExtensions.CreateVersion7();
}

public static class ApplicationExtensions
{
    internal static async Task<Guid> CreateDialog(this DialogApplication application, CreateDialogCommand command)
        => (await application.Send(command)).AsT0.DialogId;

    internal static async Task<DialogDto> GetDialog(this DialogApplication application, Guid dialogId)
        => (await application.Send(new GetDialogQuery { DialogId = dialogId })).AsT0;
}

public static class FlowStepExtensions
{
    // public static IFlowStep<CreateDialogResult> CreateSimpleDialog(this IFlowStep step, Guid dialogId)
    //     => step.SendCommand(DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId));

    public static IFlowStep<CreateDialogResult> CreateSimpleDialog(this IFlowStep step)
    {
        var context = step.Context();
        var dialogId = IdentifiableExtensions.CreateVersion7();
        context.Stuff["dialogId"] = dialogId;
        return step.SendCommand(DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId));
    }

    public static IFlowStep<GetDialogResult> GetDialog(this IFlowStep step, Guid dialogId)
        => step.SendCommand(new GetDialogQuery { DialogId = dialogId });

    public static IFlowStep<GetDialogResult> GetDialog(this IFlowStep step)
    {
        var context = step.Context();
        var dialogId = (Guid)context.Stuff["dialogId"]!;
        return step.SendCommand(new GetDialogQuery { DialogId = dialogId });
    }

    public static IFlowStep<UpdateDialogResult> UpdateDialog(
        this IFlowStep<CreateDialogResult> step,
        Action<UpdateDialogDto> modify)
    {
        return new DeferredFlowStep<UpdateDialogResult>(async () =>
        {
            var createResult = await step.ExecuteAsync();
            createResult.TryPickT0(out var createSuccess, out _).Should().BeTrue("Expected dialog creation to succeed");
            createSuccess.Should().NotBeNull();
            var dialogId = createSuccess.DialogId;

            var context = step.Context();
            var newContext = context with { Commands = [] };

            return new FlowStep<UpdateDialogResult>(newContext)
                .SendCommand(new GetDialogQuery { DialogId = dialogId })
                .Select((getResult, ctx) =>
                {
                    var dialog = getResult.AsT0;
                    return ctx.Application.GetMapper().Map<UpdateDialogDto>(dialog);
                })
                .Modify(modify)
                .SendCommand(updateDto => new UpdateDialogCommand { Id = dialogId, Dto = updateDto });
        });
    }


    // public static async Task<IFlowStep<UpdateDialogResult>> UpdateDialog(
    //     this IFlowStep<CreateDialogResult> step,
    //     Action<UpdateDialogDto> modify)
    // {
    //     var createResult = await step.ExecuteAsync();
    //     createResult.TryPickT0(out var createSuccess, out _).Should().BeTrue("Expected dialog creation to succeed");
    //     createSuccess.Should().NotBeNull();
    //     var dialogId = createSuccess.DialogId;
    //
    //     var context = step.Context();
    //     var newContext = context with { Commands = [] };
    //     return new FlowStep<UpdateDialogResult>(newContext)
    //         .SendCommand(new GetDialogQuery { DialogId = dialogId })
    //         .Select((getResult, ctx) =>
    //         {
    //             var dialog = getResult.AsT0;
    //             return ctx.Application.GetMapper().Map<UpdateDialogDto>(dialog);
    //         })
    //         .Modify(modify)
    //         .SendCommand(updateDto => new UpdateDialogCommand { Id = dialogId, Dto = updateDto });
    // }


    public static async Task<DialogDto> AssertSuccess(this IFlowStep<GetDialogResult> step)
    {
        var result = await step.ExecuteAsync();
        result.TryPickT0(out var success, out _).Should().BeTrue("Expected dialog query to return success");
        success.Should().NotBeNull();
        return success!;
    }

    // Overload to access Application inside Select
    private static IFlowStep<TOut> Select<TIn, TOut>(
        this IFlowStep<TIn> step,
        Func<TIn, FlowContext, TOut> selector)
    {
        var context = step.Context();
        return step.Select(input => selector(input, context));
    }

    // Use reflection to get FlowContext (can be replaced with a cleaner design later)
    public static FlowContext Context<T>(this IFlowStep<T> step)
    {
        var contextField = typeof(FlowStep<T>).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
        return (FlowContext)contextField!.GetValue(step)!;
    }

    public static FlowContext Context(this IFlowStep step)
    {
        // Reuse the generic version through reflection
        var field = step.GetType().GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
        return (FlowContext)field!.GetValue(step)!;
    }
}

public static class FlowBuilder
{
    public static IFlowStep For(DialogApplication application) =>
        new FlowStep<object?>(new FlowContext(application, [], []));
}

public record FlowContext(
    DialogApplication Application,
    List<Func<object?, CancellationToken, Task<object?>>> Commands,
    Dictionary<string, object?> Stuff);

public readonly struct FlowStep<TIn> : IFlowStep<TIn>
{
    private readonly FlowContext _context;

    public FlowStep(FlowContext context)
    {
        _context = context;
    }

    public IFlowStep<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector)
    {
        var context = _context;
        _context.Commands.Add(async (input, cancellationToken) =>
        {
            var command = commandSelector((TIn)input!);
            return await context.Application.Send(command, cancellationToken);
        });
        return new FlowStep<TOut>(context);
    }

    public IFlowStep<TOut> Select<TOut>(Func<TIn, TOut> selector)
    {
        var context = _context;
        _context.Commands.Add((input, _) => Task.FromResult<object?>(selector((TIn)input!)));
        return new FlowStep<TOut>(context);
    }

    public IFlowStep<TIn> Modify(Action<TIn> selector)
    {
        _context.Commands.Add((input, _) =>
        {
            selector((TIn)input!);
            return Task.FromResult(input);
        });
        return this;
    }

    public async Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        object? current = null;

        foreach (var command in _context.Commands)
        {
            current = await command(current, cancellationToken);
        }

        return (TIn)current!;
    }

    public IFlowStep<TOut> SendCommand<TOut>(IRequest<TOut> command)
    {
        var context = _context;
        _context.Commands.Add(async (_, cancellationToken) =>
            await context.Application.Send(command, cancellationToken));
        return new FlowStep<TOut>(context);
    }
}

public interface IFlowStep
{
    IFlowStep<TOut> SendCommand<TOut>(IRequest<TOut> command);
}

public interface IFlowStep<TIn> : IFlowStep
{
    IFlowStep<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector);
    IFlowStep<TOut> Select<TOut>(Func<TIn, TOut> selector);
    IFlowStep<TIn> Modify(Action<TIn> selector);
    Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class DeferredFlowStep<TIn> : IFlowStep<TIn>
{
    private readonly Func<Task<IFlowStep<TIn>>> _stepFactory;

    public DeferredFlowStep(Func<Task<IFlowStep<TIn>>> stepFactory)
    {
        _stepFactory = stepFactory;
    }

    private async Task<IFlowStep<TIn>> GetStepAsync() => await _stepFactory();


    public IFlowStep<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector) =>
        new DeferredFlowStep<TOut>(async () =>
            (await GetStepAsync()).SendCommand(commandSelector)
        );

    public IFlowStep<TOut> Select<TOut>(Func<TIn, TOut> selector) =>
        new DeferredFlowStep<TOut>(async () =>
            (await GetStepAsync()).Select(selector)
        );

    public IFlowStep<TIn> Modify(Action<TIn> selector) =>
        new DeferredFlowStep<TIn>(async () =>
            (await GetStepAsync()).Modify(selector)
        );

    public Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default) =>
        _stepFactory().Result.ExecuteAsync(cancellationToken);

    public IFlowStep<TOut> SendCommand<TOut>(IRequest<TOut> command) =>
        new DeferredFlowStep<TOut>(async () =>
            (await GetStepAsync()).SendCommand(command)
        );
}
