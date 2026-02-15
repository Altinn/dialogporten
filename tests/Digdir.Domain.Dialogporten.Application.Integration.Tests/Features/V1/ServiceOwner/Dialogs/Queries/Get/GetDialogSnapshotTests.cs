using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogSnapshotTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_ServiceOwner_Dialog_Snapshot_Test()
    {
        var result = await FlowBuilder.For(Application)
            .CreateComplexDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        await Verify(result)
            .UseDirectory("Snapshots");
    }
}
