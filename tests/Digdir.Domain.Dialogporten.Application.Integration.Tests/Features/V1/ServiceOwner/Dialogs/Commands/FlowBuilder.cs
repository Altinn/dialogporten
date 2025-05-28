using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

public static class FlowBuilder
{
    public static IFlowStep For(DialogApplication application) =>
        new FlowStep<object?>(new FlowContext(application, [], []));
}

public record FlowContext(
    DialogApplication Application,
    Dictionary<string, object?> Bag,
    List<Func<object?, CancellationToken, Task<object?>>> Commands);

public readonly struct FlowStep<TIn> : IFlowExecutor<TIn>
{
    public FlowContext Context { get; }

    public FlowStep(FlowContext context)
    {
        Context = context;
    }

    public IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector)
        => SendCommand((@in, _) => commandSelector(@in));

    public IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, FlowContext, IRequest<TOut>> commandSelector)
    {
        var context = Context;
        Context.Commands.Add(async (input, cancellationToken) =>
        {
            var command = commandSelector((TIn)input!, context);
            return await context.Application.Send(command, cancellationToken);
        });
        return new FlowStep<TOut>(context);
    }

    public IFlowExecutor<TOut> Select<TOut>(Func<TIn, TOut> selector)
        => Select((@in, _) => selector(@in));

    public IFlowExecutor<TOut> Select<TOut>(Func<TIn, FlowContext, TOut> selector)
    {
        var context = Context;
        Context.Commands.Add((input, _) => Task.FromResult<object?>(selector((TIn)input!, context)));
        return new FlowStep<TOut>(context);
    }

    public async Task<TIn> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        object? current = null;

        foreach (var command in Context.Commands)
        {
            current = await command(current, cancellationToken);
        }

        return (TIn)current!;
    }

    public IFlowExecutor<TOut> SendCommand<TOut>(IRequest<TOut> command)
    {
        var context = Context;
        Context.Commands.Add(async (_, cancellationToken) =>
            await context.Application.Send(command, cancellationToken));
        return new FlowStep<TOut>(context);
    }
}

public interface IFlowStep
{
    IFlowExecutor<TOut> SendCommand<TOut>(IRequest<TOut> command);
    FlowContext Context { get; }
}

public interface IFlowStep<out TIn> : IFlowStep
{
    IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector);
    IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, FlowContext, IRequest<TOut>> commandSelector);
    IFlowExecutor<TOut> Select<TOut>(Func<TIn, TOut> selector);
    IFlowExecutor<TOut> Select<TOut>(Func<TIn, FlowContext, TOut> selector);
}

public interface IFlowExecutor<TOut> : IFlowStep<TOut>
{
    Task<TOut> ExecuteAsync(CancellationToken cancellationToken = default);
}

public static class IFlowStepExtensions
{
    private const string DialogIdKey = "DialogId";

    public static IFlowExecutor<CreateDialogResult> CreateDialog(this IFlowStep step, CreateDialogCommand command)
    {
        step.Context.Bag[DialogIdKey] = command.Dto.Id = command.Dto.Id.CreateVersion7IfDefault();
        return step.SendCommand(command);
    }

    public static IFlowExecutor<CreateDialogResult> CreateSimpleDialog(this IFlowStep step,
        Action<CreateDialogCommand>? initialState = null)
    {
        var command = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        initialState?.Invoke(command);
        return step.CreateDialog(command);
    }

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<CreateDialogResult> step, Action<UpdateDialogDto> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x => new GetDialogQuery { DialogId = x.DialogId })
            .AssertResult<DialogDto>()
            .Select((x, ctx) =>
            {
                var updateDto = ctx.Application.GetMapper().Map<UpdateDialogDto>(x);
                modify(updateDto);
                return updateDto;
            })
            .SendCommand((x, ctx) => new UpdateDialogCommand { Id = ctx.GetDialogId(), Dto = x });

    public static IFlowExecutor<GetDialogResult> GetServiceOwnerDialog(this IFlowStep<UpdateDialogResult> step) =>
        step.AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => new GetDialogQuery { DialogId = ctx.GetDialogId() });

    public static IFlowExecutor<GetDialogResult> GetServiceOwnerDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => new GetDialogQuery { DialogId = ctx.GetDialogId() });

    public static IFlowExecutor<TIn> Modify<TIn>(
        this IFlowStep<TIn> step,
        Action<TIn> selector) => step.Modify((x, _)
        => selector(x));

    public static IFlowExecutor<TIn> Modify<TIn>(
        this IFlowStep<TIn> step,
        Action<TIn, FlowContext> selector) =>
        step.Select((@in, context) =>
        {
            selector(@in, context);
            return @in;
        });

    public static Task<T> ExecuteAndAssert<T>(this IFlowStep<IOneOf> step)
        => step.AssertResult<T>().ExecuteAsync();

    public static IFlowExecutor<T> AssertResult<T>(this IFlowStep<IOneOf> step) =>
        step.Select(result =>
        {
            var typedResult = result.Value.Should().BeOfType<T>().Subject;
            typedResult.Should().NotBeNull();
            return typedResult;
        });

    private static Guid GetDialogId(this FlowContext ctx)
    {
        ctx.Bag.TryGetValue(DialogIdKey, out var value).Should().BeTrue();
        return value.Should().BeOfType<Guid>().Subject;
    }
}
