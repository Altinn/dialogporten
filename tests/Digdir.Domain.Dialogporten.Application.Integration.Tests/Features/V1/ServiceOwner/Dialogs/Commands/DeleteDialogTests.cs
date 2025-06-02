using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeleteDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Deleting_Dialog_Should_Set_DeletedAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .SendCommand((_, ctx) => new GetDialogQuery
            {
                DialogId = ctx.GetDialogId()
            })
            .ExecuteAndAssert<DialogDto>(x =>
                x.DeletedAt.ShouldBeCloseToNow());

    [Fact]
    public Task Updating_Deleted_Dialog_Should_Return_EntityDeleted() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .SendCommand((_, ctx) => new GetDialogQuery { DialogId = ctx.GetDialogId() })
            .AssertResult<DialogDto>()
            .SendCommand(IFlowStepExtensions.CreateUpdateDialogCommand)
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>();

    [Fact]
    public async Task DeleteDialogCommand_Should_Return_New_Revision()
    {
        Guid? originalRevision = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                originalRevision = x.Revision;
            })
            .SendCommand(x => new DeleteDialogCommand { Id = x.DialogId })
            .ExecuteAndAssert<DeleteDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                x.Revision.Should().NotBe(originalRevision!.Value);
            });
    }
}
