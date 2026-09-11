using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

public interface IFlowExecutor<TOut> : IFlowStep<TOut>
{
    Task<TOut> ExecuteAsync(CancellationToken? cancellationToken = null);
}

public interface IFlowStep<out TIn> : IFlowStep
{
    IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, IRequest<TOut>> commandSelector);
    IFlowExecutor<TOut> SendCommand<TOut>(Func<TIn, FlowContext, IRequest<TOut>> commandSelector);
    IFlowExecutor<TOut> Select<TOut>(Func<TIn, TOut> selector);
    IFlowExecutor<TOut> Select<TOut>(Func<TIn, FlowContext, TOut> selector);
}

public interface IFlowStep
{
    IFlowExecutor<TOut> SendCommand<TOut>(Func<FlowContext, IRequest<TOut>> commandSelector);
    FlowContext Context { get; }
}

internal static class FlowStepExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep Do(Action<FlowContext> action)
        {
            var context = flowStep.Context;
            context.Commands.Add((x, _) =>
            {
                action.Invoke(context);
                return Task.FromResult(x);
            });
            return flowStep;
        }

        public TFlowStep Do(Action<object?, FlowContext> action)
        {
            var context = flowStep.Context;
            context.Commands.Add((x, _) =>
            {
                action.Invoke(x, context);
                return Task.FromResult(x);
            });
            return flowStep;
        }

        public TFlowStep Do(Func<FlowContext, Task> action)
        {
            var context = flowStep.Context;
            context.Commands.Add(async (x, _) =>
            {
                await action.Invoke(context);
                return x;
            });
            return flowStep;
        }

        public TFlowStep Do(Func<object?, FlowContext, Task> action)
        {
            var context = flowStep.Context;
            context.Commands.Add(async (x, _) =>
            {
                await action.Invoke(x, context);
                return x;
            });
            return flowStep;
        }
    }
}

public record FlowContext(
    DialogApplication Application,
    FlowState State,
    Dictionary<string, object?> Bag,
    List<Func<object?, CancellationToken, Task<object?>>> Commands
);

public record FlowState()
{
    public List<CommandResultPair<CreateDialogCommand, CreateDialogResult?>> CreatedDialogs { get; } = [];

    public List<CommandResultPair<CreateTransmissionCommand, CreateTransmissionResult?>> CreatedTransmissions { get; } =
        [];

    public List<CommandResultPair<CreateActivityCommand, CreateActivityResult?>> CreatedActivities { get; } = [];
}

public class CommandResultPair<TCommand, TResult>(TCommand command)
{
    public TCommand Command { get; } = command;
    public TResult? Result { get; set; }
}
