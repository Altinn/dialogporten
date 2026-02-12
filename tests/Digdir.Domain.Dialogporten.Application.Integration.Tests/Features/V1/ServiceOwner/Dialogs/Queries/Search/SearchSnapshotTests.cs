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
    public Task Search_Dialog_Verify_Output() =>
        FlowBuilder.For(Application)
            .CreateComplexDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
            })
            .GetEndUserDialog() // Trigger seen log
            .CreateComplexDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Attachments.Clear();
            })
            .CreateComplexDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Transmissions.Clear();
            })
            .CreateComplexDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
            })
            .CreateComplexDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.ServiceOwnerContext = new DialogServiceOwnerContextDto
                {
                    ServiceOwnerLabels = [new() { Value = "some-label" }]
                };
            })
            .GetEndUserDialog() // Trigger seen log
            .SearchServiceOwnerDialogs(_ => { })
            .VerifySnapshot<PaginatedList<DialogDto>>(x =>
                x.IgnoreMember(nameof(PaginatedList<>.ContinuationToken)));
}
