using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

public interface IFlowExecutor<TOut> : IFlowStep<TOut>
{
    Task<TOut> ExecuteAsync(CancellationToken cancellationToken = default);
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
    IFlowExecutor<TOut> SendCommand<TOut>(IRequest<TOut> command);
    FlowContext Context { get; }
}

public record FlowContext(
    DialogApplication Application,
    Dictionary<string, object?> Bag,
    List<Func<object?, CancellationToken, Task<object?>>> Commands);
