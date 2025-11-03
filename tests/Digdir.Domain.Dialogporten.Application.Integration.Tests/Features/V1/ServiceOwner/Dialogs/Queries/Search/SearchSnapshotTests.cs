using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using DialogServiceOwnerContextDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.DialogServiceOwnerContextDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchSnapshotTests : ApplicationCollectionFixture
{
    public SearchSnapshotTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Search_Dialog_Verify_Output()
    {
        var searchResult = await FlowBuilder.For(Application)
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
            })
            .GetEndUserDialog() // Trigger seen log
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Attachments.Clear();
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Transmissions.Clear();
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.ServiceOwnerContext = new DialogServiceOwnerContextDto
                {
                    ServiceOwnerLabels = [new() { Value = "some-label" }]
                };
            })
            .GetEndUserDialog() // Trigger seen log
            .SearchServiceOwnerDialogs(_ => { })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));

        await Verify(searchResult, settings)
            .UseDirectory("Snapshots");
    }
}
