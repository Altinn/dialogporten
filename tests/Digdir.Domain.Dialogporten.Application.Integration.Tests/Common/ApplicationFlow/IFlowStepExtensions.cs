using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
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

    public static IFlowExecutor<CreateDialogResult> CreateComplexDialog(this IFlowStep step,
        Action<CreateDialogCommand>? initialState = null)
    {
        var command = DialogGenerator.GenerateFakeCreateDialogCommand();
        initialState?.Invoke(command);
        return step.CreateDialog(command);
    }

    public static IFlowExecutor<CreateDialogResult> CreateSimpleDialog(this IFlowStep step,
        Action<CreateDialogCommand>? initialState = null)
    {
        var command = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        initialState?.Invoke(command);
        return step.CreateDialog(command);
    }

    public static IFlowExecutor<PurgeDialogResult> PurgeDialog(this IFlowStep<PurgeDialogResult> step) =>
        step.SendCommand((_, ctx) => new PurgeDialogCommand { DialogId = ctx.GetDialogId() });

    public static IFlowExecutor<PurgeDialogResult> PurgeDialog(this IFlowStep<CreateDialogResult> step,
        Action<PurgeDialogCommand>? modify = null) =>
        step.AssertResult<CreateDialogSuccess>()
            .Select(x =>
            {
                var command = new PurgeDialogCommand
                {
                    DialogId = x.DialogId,
                    IfMatchDialogRevision = x.Revision
                };
                modify?.Invoke(command);
                return command;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<DeleteDialogResult> DeleteDialog(this IFlowStep<CreateDialogSuccess> step) =>
        step.Select(x => new DeleteDialogCommand { Id = x.DialogId })
            .SendCommand(x => x);

    public static IFlowExecutor<DeleteDialogResult> DeleteDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .Select(x => new DeleteDialogCommand { Id = x.DialogId })
            .SendCommand(x => x);

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<DeleteDialogResult> step) =>
        step.AssertResult<DeleteDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetDialogQuery(ctx.GetDialogId()))
            .AssertResult<DialogDto>()
            .Select(CreateUpdateDialogCommand)
            .SendCommand(x => x);

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<CreateDialogResult> step, Action<UpdateDialogCommand> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x => CreateGetDialogQuery(x.DialogId))
            .AssertResult<DialogDto>()
            .Select((x, ctx) =>
            {
                var command = CreateUpdateDialogCommand(x, ctx);
                modify(command);
                return command;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<GetDialogResult> GetServiceOwnerDialog(this IFlowStep<UpdateDialogResult> step) =>
        step.AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResult> GetServiceOwnerDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResult> GetServiceOwnerDialog(this IFlowStep<DeleteDialogResult> step) =>
        step.SendCommand((_, ctx) => CreateGetDialogQuery(ctx.GetDialogId()));

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

    public static Task<object> ExecuteAndAssert(this IFlowStep<IOneOf> step, Type type)
        => step.Select(result =>
            {
                result.Value.Should().BeOfType(type).And.NotBeNull();
                return result.Value;
            })
            .ExecuteAsync();

    public static Task<T> ExecuteAndAssert<T>(this IFlowStep<IOneOf> step, Action<T>? assert = null)
        => step.AssertResult(assert).ExecuteAsync();

    public static IFlowExecutor<T> AssertResult<T>(this IFlowStep<IOneOf> step, Action<T>? assert = null) =>
        step.Select(result =>
        {
            var typedResult = result.Value.Should().BeOfType<T>().Subject;
            typedResult.Should().NotBeNull();
            assert?.Invoke(typedResult);
            return typedResult;
        });

    private static Guid GetDialogId(this FlowContext ctx)
    {
        ctx.Bag.TryGetValue(DialogIdKey, out var value).Should().BeTrue();
        return value.Should().BeOfType<Guid>().Subject;
    }

    private static UpdateDialogCommand CreateUpdateDialogCommand(DialogDto dto, FlowContext ctx)
    {
        var updateDto = ctx.Application.GetMapper().Map<UpdateDialogDto>(dto);
        return new UpdateDialogCommand
        {
            IfMatchDialogRevision = dto.Revision,
            Id = ctx.GetDialogId(),
            Dto = updateDto
        };
    }

    private static GetDialogQuery CreateGetDialogQuery(Guid id) => new() { DialogId = id };
}
