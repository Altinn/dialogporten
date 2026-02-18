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
    }
}

public record FlowContext(
    DialogApplication Application,
    Dictionary<string, object?> Bag,
    List<Func<object?, CancellationToken, Task<object?>>> Commands);
