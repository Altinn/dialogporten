using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using OneOf;
using OneOf.Types;
using GetDialogQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery;
using GetDialogResultEU =
    Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogResult;
using SearchDialogResultEU =
    Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogResult;
using SearchDialogQueryEU =
    Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogQuery;
using GetDialogQuerySO =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogQuery;
using GetDialogResultSO =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogResult;
using SearchDialogResultSO =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.SearchDialogResult;
using SearchDialogQuerySO =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.SearchDialogQuery;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

public static class IFlowStepExtensions
{
    private const string DialogIdKey = "DialogId";

    public static IFlowExecutor<CreateDialogResult> CreateDialog(this IFlowStep step, CreateDialogCommand command)
    {
        step.Context.Bag[DialogIdKey] = command.Dto.Id = command.Dto.Id.CreateVersion7IfDefault();
        return step.SendCommand(command);
    }

    public static IFlowExecutor<CreateDialogResult> CreateDialogs(this IFlowStep step, List<CreateDialogCommand> commands)
    {
        if (commands.Count == 0)
        {
            throw new ArgumentException("At least one command is required to create dialogs.", nameof(commands));
        }

        for (var i = 0; i < commands.Count - 1; i++)
        {
            step = step
                .SendCommand(commands[i])
                .AssertResult<CreateDialogSuccess>();
        }

        return step.SendCommand(commands[^1]);
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


    public static IFlowExecutor<PurgeDialogResult> PurgeDialog(this IFlowStep<RestoreDialogResult> step,
        Action<PurgeDialogCommand>? modify = null) =>
        step.AssertResult<RestoreDialogSuccess>()
            .Select((_, ctx) =>
            {
                var command = new PurgeDialogCommand
                {
                    DialogId = ctx.GetDialogId(),
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

    public static IFlowExecutor<DeleteDialogResult> DeleteDialog(this IFlowStep<UpdateDialogResult> step, Action<DeleteDialogCommand>? modify = null) =>
        step.AssertResult<UpdateDialogSuccess>()
            .Select((_, ctx) =>
            {
                var command = new DeleteDialogCommand { Id = ctx.GetDialogId() };
                modify?.Invoke(command);
                return command;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<RestoreDialogResult> RestoreDialog(this IFlowStep<DeleteDialogResult> step, Action<RestoreDialogCommand>? modify = null) =>
        step.AssertResult<DeleteDialogSuccess>()
            .Select((_, ctx) =>
            {
                var command = new RestoreDialogCommand { DialogId = ctx.GetDialogId() };
                modify?.Invoke(command);
                return command;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<DeleteDialogResult> step) =>
        step.AssertResult<DeleteDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()))
            .AssertResult<DialogDto>()
            .Select(CreateUpdateDialogCommand)
            .SendCommand(x => x);

    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<CreateDialogResult> step,
        Action<UpdateDialogCommand> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x => CreateGetServiceOwnerDialogQuery(x.DialogId))
            .AssertResult<DialogDto>()
            .Select((x, ctx) =>
            {
                var command = CreateUpdateDialogCommand(x, ctx);
                modify(command);
                return command;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<UpdateDialogResult> step) =>
        step.AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<DeleteDialogResult> step) =>
        step.SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultEU> GetEndUserDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetEndUserDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultEU> GetEndUserDialog(this IFlowStep<DeleteDialogResult> step) =>
        step.SendCommand((_, ctx) => CreateGetEndUserDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<SearchDialogResultSO> SearchServiceOwnerDialogs(this IFlowStep step,
        Action<SearchDialogQuerySO> modify)
    {
        var query = new SearchDialogQuerySO();
        modify(query);
        return step.SendCommand(query);
    }
    public static IFlowExecutor<SearchDialogResultSO> SearchServiceOwnerDialogs(this IFlowStep<CreateDialogResult> step,
        Action<SearchDialogQuerySO> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .Select(_ =>
            {
                var query = new SearchDialogQuerySO();
                modify(query);
                return query;
            })
            .SendCommand(x => x);

    public static IFlowExecutor<SearchDialogResultEU> SearchEndUserDialogs(this IFlowStep step,
        Action<SearchDialogQueryEU> modify)
    {
        var query = new SearchDialogQueryEU();
        modify(query);
        return step.SendCommand(query);
    }

    public static IFlowExecutor<SearchDialogResultEU> SearchEndUserDialogs(this IFlowStep<CreateDialogResult> step,
        Action<SearchDialogQueryEU> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .Select(_ =>
            {
                var query = new SearchDialogQueryEU();
                modify(query);
                return query;
            })
            .SendCommand(x => x);

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

    public static Guid GetDialogId(this FlowContext ctx)
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

    private static GetDialogQuerySO CreateGetServiceOwnerDialogQuery(Guid id) => new() { DialogId = id };
    private static GetDialogQueryEU CreateGetEndUserDialogQuery(Guid id) => new() { DialogId = id };
}
