using System.Data;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class IfMatchConcurrencyEarlyExitTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task UpdateDialog_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .UpdateDialog(x => x.IfMatchDialogRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task DeleteDialog_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .DeleteDialog(x => x.IfMatchDialogRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task PurgeDialog_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .PurgeDialog(x => x.IfMatchDialogRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task RestoreDialog_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .RestoreDialog(x => x.IfMatchDialogRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task CreateTransmission_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .CreateTransmission(_ => { },
                ifMatchDialogRevision: Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task UpdateServiceOwnerContext_Returns_ConcurrencyError_On_IfMatchServiceOwnerContextRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .UpdateServiceOwnerContext(x =>
                x.IfMatchServiceOwnerContextRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task FreezeDialog_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(x =>
            {
                DecorateUserAsAdmin(x);
                DecorateSaveChangesForbidden(x);
            })
            .SendCommand((_, ctx) => new FreezeDialogCommand
            {
                Id = ctx.GetDialogId(),
                IfMatchDialogRevision = Guid.NewGuid()
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task UpdateFormSavedActivityTime_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch()
    {
        var activityId = Guid.CreateVersion7();
        return FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.AddActivity(DialogActivityType.Values.FormSaved, a => a.Id = activityId))
            .ConfigureServices(x =>
            {
                DecorateUserAsAdmin(x);
                DecorateSaveChangesForbidden(x);
            })
            .SendCommand((_, ctx) => new UpdateFormSavedActivityTimeCommand
            {
                DialogId = ctx.GetDialogId(),
                ActivityId = activityId,
                NewCreatedAt = DateTimeOffset.UtcNow,
                IfMatchDialogRevision = Guid.NewGuid()
            })
            .ExecuteAndAssert<ConcurrencyError>();
    }

    [Fact]
    public Task SetSystemLabelsServiceOwner_Returns_ConcurrencyError_On_IfMatchEndUserContextRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .SetSystemLabelsServiceOwner(x =>
            {
                x.IfMatchEndUserContextRevision = Guid.NewGuid();
                x.AddLabels = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task BulkSetSystemLabelsServiceOwner_Returns_ConcurrencyError_On_EndUserContextRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new()
                        {
                            DialogId = ctx.GetDialogId(),
                            EndUserContextRevision = Guid.NewGuid()
                        }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                };
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task SetSystemLabelsEndUser_Returns_ConcurrencyError_On_IfMatchEndUserContextRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .SetSystemLabelsEndUser(x =>
            {
                x.IfMatchEndUserContextRevision = Guid.NewGuid();
                x.AddLabels = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task BulkSetSystemLabelsEndUser_Returns_ConcurrencyError_On_EndUserContextRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(DecorateSaveChangesForbidden)
            .BulkSetSystemLabelEndUser((x, ctx) =>
            {
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new()
                        {
                            DialogId = ctx.GetDialogId(),
                            EndUserContextRevision = Guid.NewGuid()
                        }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                };
            })
            .ExecuteAndAssert<ConcurrencyError>();

    private static void DecorateSaveChangesForbidden(IServiceCollection services) =>
        services.Decorate<IUnitOfWork, SaveChangesForbiddenUnitOfWork>();

    private static void DecorateUserAsAdmin(IServiceCollection services) =>
        services.Decorate<IUserResourceRegistry, AdminUserResourceRegistryDecorator>();
}

// Decorating IUnitOfWork to throw an exception when SaveChangesAsync is called to verify early-exit concurrency checks
internal sealed class SaveChangesForbiddenUnitOfWork(IUnitOfWork inner) : IUnitOfWork
{
    private readonly IUnitOfWork _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public Task<SaveChangesResult> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("SaveChangesAsync should not be called for early-exit concurrency checks.");

    public IUnitOfWork EnableConcurrencyCheck<TEntity>(TEntity? entity, Guid? revision)
        where TEntity : class, IVersionableEntity
    {
        _inner.EnableConcurrencyCheck(entity, revision);
        return this;
    }

    public Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        CancellationToken cancellationToken = default) =>
        _inner.BeginTransactionAsync(isolationLevel, cancellationToken);

    public IUnitOfWork DisableAggregateFilter()
    {
        _inner.DisableAggregateFilter();
        return this;
    }

    public IUnitOfWork DisableVersionableFilter()
    {
        _inner.DisableVersionableFilter();
        return this;
    }

    public IUnitOfWork DisableUpdatableFilter()
    {
        _inner.DisableUpdatableFilter();
        return this;
    }

    public IUnitOfWork DisableSoftDeletableFilter()
    {
        _inner.DisableSoftDeletableFilter();
        return this;
    }

    public IUnitOfWork DisableImmutableFilter()
    {
        _inner.DisableImmutableFilter();
        return this;
    }
}
