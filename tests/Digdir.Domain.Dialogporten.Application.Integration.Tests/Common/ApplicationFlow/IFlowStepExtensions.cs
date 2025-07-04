using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using OneOf;
using DialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;
using DialogDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using GetDialogQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery;
using GetDialogResultEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogResult;
using SearchDialogResultEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogResult;
using SearchDialogQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogQuery;
using GetDialogQuerySO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogQuery;
using GetDialogResultSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogResult;
using SearchDialogResultSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.SearchDialogResult;
using SearchDialogQuerySO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.SearchDialogQuery;
using BulkSetSystemLabelResultEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelResult;
using BulkSetSystemLabelCommandEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand;
using BulkSetSystemLabelResultSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelResult;
using BulkSetSystemLabelCommandSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

public static class IFlowStepExtensions
{
    private const string DialogIdKey = "DialogId";
    private const string PartyKey = "Party";
    private const string ServiceResource = "ServiceResource";

    public static IFlowExecutor<CreateDialogSuccess> CreateDialogs(this IFlowStep step,
        params CreateDialogCommand[] commands)
    {
        foreach (var command in commands)
        {
            step = step
                .SendCommand(_ => command)
                .AssertResult<CreateDialogSuccess>();
        }

        return step as IFlowExecutor<CreateDialogSuccess>
               ?? throw new ArgumentException("At least one command is required to create dialogs.", nameof(commands));
    }

    public static IFlowExecutor<CreateDialogResult> CreateDialog(this IFlowStep step, Func<FlowContext, CreateDialogCommand> commandSelector) =>
        step.SendCommand(ctx =>
        {
            var command = commandSelector(ctx);
            ctx.Bag[DialogIdKey] = command.Dto.Id = command.Dto.Id.CreateVersion7IfDefault();
            ctx.Bag[ServiceResource] = command.Dto.ServiceResource;
            ctx.Bag[PartyKey] = command.Dto.Party;
            return command;
        });

    public static IFlowExecutor<CreateDialogResult> CreateComplexDialog(this IFlowStep step,
        Action<CreateDialogCommand>? initialState = null) =>
        step.CreateDialog(_ =>
        {
            var command = DialogGenerator.GenerateFakeCreateDialogCommand();
            initialState?.Invoke(command);
            return command;
        });

    public static IFlowExecutor<CreateDialogResult> CreateSimpleDialog(this IFlowStep step,
        Action<CreateDialogCommand>? initialState = null) =>
        step.CreateDialog(_ =>
        {
            var command = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
            initialState?.Invoke(command);
            return command;
        });

    public static IFlowExecutor<PurgeDialogResult> PurgeDialog(this IFlowStep<CreateDialogResult> step,
        Action<PurgeDialogCommand>? modify = null) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x =>
            {
                var command = new PurgeDialogCommand
                {
                    DialogId = x.DialogId,
                    IfMatchDialogRevision = x.Revision
                };
                modify?.Invoke(command);
                return command;
            });

    public static IFlowExecutor<DeleteDialogResult> DeleteDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(x => new DeleteDialogCommand { Id = x.DialogId });

    public static IFlowExecutor<RestoreDialogResult> RestoreDialog(this IFlowStep<DeleteDialogResult> step,
        Action<RestoreDialogCommand>? modify = null) =>
        step.AssertResult<DeleteDialogSuccess>()
            .SendCommand((_, ctx) =>
            {
                var command = new RestoreDialogCommand { DialogId = ctx.GetDialogId() };
                modify?.Invoke(command);
                return command;
            });

    public static IFlowExecutor<UpdateDialogResult> AssertSuccessAndUpdateDialog(this IFlowStep<IOneOf> step,
        Action<UpdateDialogCommand> modify) =>
        step.AssertSuccess()
            .SendCommand(x => CreateGetServiceOwnerDialogQuery(x.GetDialogId()))
            .AssertResult<DialogDtoSO>()
            .SendCommand((x, ctx) =>
            {
                var command = CreateUpdateDialogCommand(x, ctx);
                modify(command);
                return command;
            });
    public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep step,
        Action<UpdateDialogCommand> modify) =>
        step.SendCommand(x => CreateGetServiceOwnerDialogQuery(x.GetDialogId()))
            .AssertResult<DialogDtoSO>()
            .SendCommand((x, ctx) =>
            {
                var command = CreateUpdateDialogCommand(x, ctx);
                modify(command);
                return command;
            });

    // public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<CreateDialogResult> step,
    //     Action<UpdateDialogCommand> modify) =>
    //     step.AssertResult<CreateDialogSuccess>()
    //         .SendCommand(x => CreateGetServiceOwnerDialogQuery(x.DialogId))
    //         .AssertResult<DialogDtoSO>()
    //         .SendCommand((x, ctx) =>
    //         {
    //             var command = CreateUpdateDialogCommand(x, ctx);
    //             modify(command);
    //             return command;
    //         });

    // public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<DialogDtoSO> step,
    //     Action<UpdateDialogCommand> modify) => step
    //     .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()))
    //     .AssertResult<DialogDtoSO>()
    //     .SendCommand((x, ctx) =>
    //     {
    //         var command = CreateUpdateDialogCommand(x, ctx);
    //         modify(command);
    //         return command;
    //     });

    // public static IFlowExecutor<UpdateDialogResult> UpdateDialog(this IFlowStep<DialogDtoEU> step,
    //     Action<UpdateDialogCommand> modify) => step
    //         .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()))
    //         .AssertResult<DialogDtoSO>()
    //         .SendCommand((x, ctx) =>
    //         {
    //             var command = CreateUpdateDialogCommand(x, ctx);
    //             modify(command);
    //             return command;
    //         });

    public static IFlowExecutor<UpdateDialogServiceOwnerContextResult> UpdateServiceOwnerContext(this IFlowStep<CreateDialogResult> step,
        Action<UpdateDialogServiceOwnerContextCommand> modify) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) =>
            {
                var command = new UpdateDialogServiceOwnerContextCommand
                {
                    DialogId = ctx.GetDialogId()
                };
                modify(command);
                return command;
            });

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<UpdateDialogResult> step) =>
        step.AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<UpdateDialogServiceOwnerContextResult> step) =>
        step.AssertResult<UpdateServiceOwnerContextSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultSO> GetServiceOwnerDialog(this IFlowStep<UpdateFormSavedActivityTimeResult> step) =>
        step.AssertResult<UpdateFormSavedActivityTimeSuccess>()
            .SendCommand((_, ctx) => CreateGetServiceOwnerDialogQuery(ctx.GetDialogId()));

    public static IFlowExecutor<GetDialogResultEU> GetEndUserDialog(this IFlowStep<CreateDialogResult> step) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => new GetDialogQueryEU { DialogId = ctx.GetDialogId() });

    public static IFlowExecutor<GetDialogResultEU> GetEndUserDialog(this IFlowStep<UpdateDialogResult> step) =>
        step.AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => new GetDialogQueryEU { DialogId = ctx.GetDialogId() });

    public static IFlowExecutor<SearchDialogResultSO> SearchServiceOwnerDialogs(this IFlowStep step,
        Action<SearchDialogQuerySO> modify)
    {
        return step.SendCommand(_ =>
        {
            var query = new SearchDialogQuerySO();
            modify(query);
            return query;
        });
    }

    public static IFlowExecutor<SearchDialogResultEU> SearchEndUserDialogs(this IFlowStep step,
        Action<SearchDialogQueryEU> modify)
    {
        return step.SendCommand(_ =>
        {
            var query = new SearchDialogQueryEU();
            modify(query);
            return query;
        });
    }

    public static IFlowExecutor<BulkSetSystemLabelResultEU> BulkSetSystemLabelEndUser(
        this IFlowStep<CreateDialogResult> step, Action<BulkSetSystemLabelCommandEU, FlowContext> modify) =>
        step.SendCommand((_, ctx) =>
        {
            var command = new BulkSetSystemLabelCommandEU { Dto = new() };
            modify(command, ctx);
            return command;
        });

    public static IFlowExecutor<BulkSetSystemLabelResultSO> BulkSetSystemLabelServiceOwner(
        this IFlowStep<CreateDialogResult> step, Action<BulkSetSystemLabelCommandSO, FlowContext> modify) =>
        step.SendCommand((_, ctx) =>
        {
            var command = new BulkSetSystemLabelCommandSO { Dto = new() };
            modify(command, ctx);
            return command;
        });

    public static IFlowExecutor<UpdateFormSavedActivityTimeResult> UpdateFormSavedActivityTime(
        this IFlowStep<CreateDialogResult> step,
        Guid activityId,
        DateTimeOffset? newCreatedAt = null) =>
        step.AssertResult<CreateDialogSuccess>()
            .SendCommand(ctx => new UpdateFormSavedActivityTimeCommand
            {
                DialogId = ctx.GetDialogId(),
                ActivityId = activityId,
                NewCreatedAt = newCreatedAt ?? DateTimeOffset.UtcNow
            });

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

    public static IFlowStep AssertSuccess(this IFlowStep<IOneOf> step) =>
        step.Select(result =>
        {
            result.Index.Should().Be(0);
            var typedResult = result.Value.Should().BeOfType<object>().Subject;
            typedResult.Should().NotBeNull();
            return typedResult;
        });

    public static Guid GetDialogId(this FlowContext ctx)
    {
        ctx.Bag.TryGetValue(DialogIdKey, out var value).Should().BeTrue();
        return value.Should().BeOfType<Guid>().Subject;
    }

    public static string GetParty(this FlowContext ctx)
    {
        ctx.Bag.TryGetValue(PartyKey, out var value).Should().BeTrue();
        return value.Should().BeOfType<string>().Subject;
    }

    public static string GetServiceResource(this FlowContext ctx)
    {
        ctx.Bag.TryGetValue(ServiceResource, out var value).Should().BeTrue();
        return value.Should().BeOfType<string>().Subject;
    }

    public static UpdateDialogCommand CreateUpdateDialogCommand(DialogDtoSO dto, FlowContext ctx)
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
}
