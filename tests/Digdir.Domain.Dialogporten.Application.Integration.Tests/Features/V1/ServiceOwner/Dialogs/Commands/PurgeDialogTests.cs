using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class PurgeDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Purge_RemovesDialog_FromDatabase()
    {
        await FlowBuilder.For(Application)
            .CreateComplexDialog()
            .PurgeDialog()
            .ExecuteAndAssert<Success>();

        var dialogEntities = await Application.GetDbEntities<DialogEntity>();
        Assert.Empty(dialogEntities);

        var dialogAttachments = await Application.GetDbEntities<DialogAttachment>();
        Assert.Empty(dialogAttachments);

        var dialogActivities = await Application.GetDbEntities<DialogActivity>();
        Assert.Empty(dialogActivities);
    }

    [Fact]
    public Task Purge_Returns_ConcurrencyError_On_IfMatchDialogRevision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .PurgeDialog(x => x.IfMatchDialogRevision = Guid.NewGuid())
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task Purge_ReturnsNotFound_OnNonExistingDialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .PurgeDialog()
            .SendCommand((_, ctx) => new PurgeDialogCommand { DialogId = ctx.GetDialogId() })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();
}
