using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Create_Sets_Default_System_Labels_Mask_On_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .Do(async (_, ctx) =>
            {
                var dialogs = await Application.GetDbEntities<DialogEntity>();
                dialogs.Should().ContainSingle(x =>
                    x.Id == ctx.GetDialogId() &&
                    x.SystemLabelsMask == 1);
            })
            .ExecuteAsync();

    [Fact]
    public Task Set_Updates_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.Bin])
            .SendCommand((x, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                {
                    x.AddLabels = [SystemLabel.Values.Bin];
                    x.IfMatchEndUserContextRevision = Guid.NewGuid();
                })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task Set_Returns_Forbidden_On_Unauthorized() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization
                    .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(false);
            })
            .SetSystemLabelsEndUser(x =>
                {
                    x.AddLabels = [SystemLabel.Values.Bin];
                })
            .ExecuteAndAssert<Forbidden>();

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        Guid? dialogId = NewUuidV7();
        Guid? revision = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x => revision = x.EndUserContext.Revision);

        await FlowBuilder.For(Application)
            .SendCommand(_ => new SetSystemLabelCommand
            {
                DialogId = dialogId.Value,
                IfMatchEndUserContextRevision = revision!.Value,
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(_ => GetDialog(dialogId))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public async Task Can_Set_And_Remove_MarkedAsUnopened_Label()
    {
        var dialogId = NewUuidV7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        var dialogSystemLabels = await Application
            .GetDbEntities<DialogEndUserContextSystemLabel>();

        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.MarkedAsUnopened);
        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.Default);

        await FlowBuilder.For(Application)
            .SendCommand(_ => new SetSystemLabelCommand
            {
                RemoveLabels = [SystemLabel.Values.MarkedAsUnopened],
                DialogId = dialogId
            })
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        dialogSystemLabels = await Application
            .GetDbEntities<DialogEndUserContextSystemLabel>();

        dialogSystemLabels.Should().NotContain(x => x.SystemLabelId == SystemLabel.Values.MarkedAsUnopened);
        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.Default);

        var dialogs = await Application.GetDbEntities<DialogEntity>();
        dialogs.Should().ContainSingle(x =>
            x.Id == dialogId &&
            x.SystemLabelsMask == 1);
    }

    [Fact]
    public async Task Set_Updates_Dialog_System_Labels_Mask()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        short expectedMask = 1 | (1 << ((int)SystemLabel.Values.MarkedAsUnopened - 1));
        var dialogs = await Application.GetDbEntities<DialogEntity>();

        dialogs.Should().ContainSingle(x =>
            x.Id == dialogId &&
            x.SystemLabelsMask == expectedMask);
    }

    [Fact]
    public Task Cannot_Set_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Cannot_Remove_Existing_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Submission))
            .SetSystemLabelsEndUser(x =>
                x.RemoveLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Set_Adds_Three_LabelLog_Entries_When_Changing_NonDefault_Label_Twice() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Archive])
            .SendCommand(ctx => new SearchLabelAssignmentLogQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                var actorNameEntities = Application.GetDbEntities<ActorName>()
                    .GetAwaiter().GetResult();
                actorNameEntities.Should().ContainSingle();

                var actorName = actorNameEntities.Single();
                x.Should().HaveCount(3)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorName.Should().Be(actorName.Name);
                    }).And.HaveCount(3);
            });

    [Fact]
    public async Task Set_As_SystemUser_Records_The_Correct_Assignment_Log()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsSystemUser()
            .SetSystemLabelsEndUser(command =>
            {
                command.AddLabels = [SystemLabel.Values.Archive];
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should().ContainSingle(label => label == SystemLabel.Values.Archive));

        using var scope = Application.GetServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDialogDbContext>();
        var expectedLabelName = SystemLabel.Values.Archive.ToNamespacedName();

        var log = await dbContext.LabelAssignmentLogs
            .Include(x => x.PerformedBy)
            .ThenInclude(x => x.ActorNameEntity)
            .SingleAsync(x => x.Name == expectedLabelName, TestContext.Current.CancellationToken);

        log.PerformedBy.ActorTypeId.Should().Be(ActorType.Values.PartyRepresentative);
        log.PerformedBy.ActorNameEntity.Should().NotBeNull();
        log.PerformedBy.ActorNameEntity.ActorId.Should().Be(TestUsers.DefaultSystemUserUrn);
        log.PerformedBy.ActorNameEntity.Name.Should().Be("Mock system user name");
    }

    [Fact]
    public Task Search_Handles_Legacy_Log_Entry_Missing_Actor_Row() =>
        // Finding A in issue #4340: LabelAssignmentLog rows written before the
        // shared-actor fix in #3553 can lack their Actor row entirely. The search
        // must tolerate such rows instead of dereferencing the missing PerformedBy.
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .Do(async ctx =>
            {
                // Simulate the legacy data state by deleting a single log entry's actor row.
                using var scope = Application.GetServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                var dialogId = ctx.GetDialogId();
                var logId = await db.LabelAssignmentLogs
                    .Where(l => l.Context.DialogId == dialogId)
                    .Select(l => l.Id)
                    .FirstAsync(TestContext.Current.CancellationToken);
                var deleted = await db.Database.ExecuteSqlAsync($"""
                                                                 DELETE FROM "Actor"
                                                                 WHERE "LabelAssignmentLogId" = {logId}
                                                                 """);
                deleted.Should().Be(1);
            })
            .SendCommand(ctx => new SearchLabelAssignmentLogQuery
            {
                DialogId = ctx.GetDialogId(),
            }).ExecuteAndAssert<List<LabelAssignmentLogDto>>(dtoList =>
            {
                dtoList.Should().ContainSingle(dto =>
                    dto.PerformedBy.ActorName == "" &&
                    dto.PerformedBy.ActorId == "" &&
                    dto.PerformedBy.ActorType == ActorType.Values.PartyRepresentative);
            });

    [Fact]
    public Task Search_Returns_Entries_When_No_Main_Resource_Access_But_List_Authorization() =>
        //When the user lacks main-resource access but is granted access via the list
        // authorization, the entries should be returned instead of NotFound.
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                // No authorized actions => no access to the main resource
                altinnAuthorization
                    .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(new DialogDetailsAuthorizationResult { AuthorizedChecks = [] });
                // But access is granted via the list authorization
                altinnAuthorization
                    .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(true);
                altinnAuthorization
                    .UserHasRequiredAuthLevel(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(true);
            })
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x => x.Should().ContainSingle());

    [Fact]
    public Task Search_Returns_NotFound_When_No_Main_Resource_Access_And_No_List_Authorization() =>
        // When the user has neither main-resource access nor list authorization,
        // the dialog is not visible and the label log must return NotFound.
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization
                    .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(new DialogDetailsAuthorizationResult { AuthorizedChecks = [] });
                altinnAuthorization
                    .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(false);
            })
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
