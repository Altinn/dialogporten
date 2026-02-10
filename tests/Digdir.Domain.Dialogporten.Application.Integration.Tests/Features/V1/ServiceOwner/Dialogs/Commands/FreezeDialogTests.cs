using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class FreezeDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Cannot_Update_Frozen_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsAdminUser()
            .SendCommand((_, ctx) => new FreezeDialogCommand { Id = ctx.GetDialogId() })
            .AssertSuccess()
            .AsIntegrationTestUser()
            .UpdateDialog(x => x.Dto.Progress = 98)
            .ExecuteAndAssert<Forbidden>();


    [Fact]
    public async Task FreezeDialogCommand_Should_Return_New_Revision()
    {
        Guid? originalRevision = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                originalRevision = x.Revision;
            })
            .AsAdminUser()
            .SendCommand((_, ctx) => new FreezeDialogCommand
            {
                Id = ctx.GetDialogId()
            })
            .ExecuteAndAssert<FreezeDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                x.Revision.Should().NotBe(originalRevision!.Value);
            });
    }
}
