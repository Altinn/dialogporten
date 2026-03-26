using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
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
            .VerifySnapshot(x =>
                x.IgnoreMember(nameof(PaginatedList<>.ContinuationToken)))
            .ExecuteAsync();

    [Fact]
    public Task Search_Latest_Activity_Verify_Output() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(AddActivities(DialogActivityType.Values.DialogCreated))
            .CreateSimpleDialog(AddActivities(DialogActivityType.Values.DialogOpened))
            .CreateSimpleDialog(AddActivities(DialogActivityType.Values.DialogDeleted))
            .AssertResult<CreateDialogSuccess>()
            .SearchServiceOwnerDialogs(_ => { })
            .VerifySnapshot(x =>
                x.IgnoreMember(nameof(PaginatedList<>.ContinuationToken)))
            .ExecuteAsync();

    private static Action<CreateDialogCommand, FlowContext> AddActivities(DialogActivityType.Values type) =>
        (x, _) =>
            x.AddActivity(modify: activity =>
                {
                    activity.Type = DialogActivityType.Values.DialogRestored;
                    activity.CreatedAt = DialogApplication.Clock.UtcNowOffset.AddDays(-2);
                })
                .AddActivity(modify: activity =>
                {
                    activity.Type = type;
                    activity.CreatedAt = DialogApplication.Clock.UtcNowOffset.AddDays(-1);
                });
}
