using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;


[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchSnapshotTests : ApplicationCollectionFixture
{
    public SearchSnapshotTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Search_Dialog_Verify_Output()
    {
        var searchResult = await FlowBuilder.For(Application)
            .CreateComplexDialog(x => x.Dto.Activities.Clear())
            .GetEndUserDialog() // Trigger seen log
            .CreateComplexDialog(x => x.Dto.Attachments.Clear())
            .CreateComplexDialog(x => x.Dto.Transmissions.Clear())
            .CreateComplexDialog(x => x.Dto.SystemLabel = SystemLabel.Values.Archive)
            .CreateComplexDialog(x => x.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = "some-label" }])
            .SearchServiceOwnerDialogs(_ => { })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));

        await Verify(searchResult, settings)
            .UseDirectory("Snapshots");
    }
}
