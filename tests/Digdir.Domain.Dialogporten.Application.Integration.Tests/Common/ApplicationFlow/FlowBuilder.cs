using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

public static class FlowBuilder
{
    public static IFlowStep For(DialogApplication application, Action<IServiceCollection>? appConfig = null)
    {
        if (appConfig is not null)
        {
            application.ConfigureServices(appConfig);
        }
        return new FlowStep<object?>(new FlowContext(application, [], []));
    }
}

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
        context.Commands.Add((input, cancellationToken) =>
        {
            var command = commandSelector((TIn)input!, context);
            return context.Application
                .Send(command, cancellationToken)
                .ContinueWith(t => (object?)t.Result, cancellationToken);
        });
        return new FlowStep<TOut>(context);
    }

    public IFlowExecutor<TOut> Select<TOut>(Func<TIn, TOut> selector)
        => Select((@in, _) => selector(@in));

    public IFlowExecutor<TOut> Select<TOut>(Func<TIn, FlowContext, TOut> selector)
    {
        var context = Context;
        context.Commands.Add((input, _) => Task.FromResult<object?>(selector((TIn)input!, context)));
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

    public IFlowExecutor<TOut> SendCommand<TOut>(Func<FlowContext, IRequest<TOut>> commandSelector)
    {
        var context = Context;
        context.Commands.Add(async (_, cancellationToken) =>
            await context.Application.Send(commandSelector(context), cancellationToken));
        return new FlowStep<TOut>(context);
    }
}
