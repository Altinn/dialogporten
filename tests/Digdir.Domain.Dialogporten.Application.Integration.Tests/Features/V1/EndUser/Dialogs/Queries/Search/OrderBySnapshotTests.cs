using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class OrderBySnapshotTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private static DateTimeOffset DateAnchor => new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    [Theory]
    [InlineData("createdAt")]
    public async Task OrderBy_Search_Verify_Output(string orderBy)
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(1);
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(2);
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(3);
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(4);
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(5);
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;
                x.Dto.CreatedAt = x.Dto.UpdatedAt = DateAnchor.AddDays(6);
            })
            .ExecuteAndAssert(_ => { });


        var searchResultPage1 = await FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Limit = 2;
                x.Party = [IntegrationTestUser.DefaultParty];
                x.OrderBy =
                    OrderSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>.TryParse(orderBy,
                        out var lala)
                        ? lala
                        : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(2);
            });

        var searchResultPage2 = await FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Limit = 2;
                x.Party = [IntegrationTestUser.DefaultParty];
                x.ContinuationToken = ContinuationTokenSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>
                    .TryParse(searchResultPage1.ContinuationToken, out var token) ? token : null;
                x.OrderBy =
                    OrderSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>.TryParse(orderBy,
                        out var lala)
                        ? lala
                        : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(2);
            });

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));
        settings.UseFileName($"OrderBySnapshotTests.{orderBy}");

        await Verify(searchResultPage1, settings)
            .UseDirectory("Snapshots")
            .DontScrubDateTimes();
    }
}
