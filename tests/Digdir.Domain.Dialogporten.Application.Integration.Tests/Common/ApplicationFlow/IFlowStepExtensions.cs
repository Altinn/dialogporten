using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

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

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<CreateDialogResult> step, Action<UpdateDialogCommand> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x => new GetDialogQuery { DialogId = x.DialogId })
            .AssertResult<DialogDto>()
            .Select((x, ctx) =>
            {
                var updateDto = ctx.Application.GetMapper().Map<UpdateDialogDto>(x);
                var command = new UpdateDialogCommand { Id = ctx.GetDialogId(), Dto = updateDto };
                modify(command);
                return command;
            })
            .SendCommand(x => x);

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
