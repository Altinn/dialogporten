using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogSnapshotTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private readonly Guid _seed = Guid.Parse("915d005a-efd1-4ede-886a-f5889a0d3202");

    [Fact]
    public Task Get_ServiceOwner_Dialog_Snapshot_Test() =>
        FlowBuilder.For(Application)
            .CreateComplexDialog(seed: _seed.GetHashCode())
            .GetServiceOwnerDialog()
            .VerifySnapshot()
            .ExecuteAndAssert<DialogDto>();
}
