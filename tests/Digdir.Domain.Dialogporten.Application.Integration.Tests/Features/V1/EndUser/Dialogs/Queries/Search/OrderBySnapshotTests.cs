using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class OrderBySnapshotTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Theory]
    [InlineData("createdAt")]
    [InlineData("createdAt_asc")]
    [InlineData("updatedAt")]
    [InlineData("updatedAt_asc")]
    [InlineData("contentUpdatedAt")]
    [InlineData("contentUpdatedAt_asc")]
    [InlineData("dueAt")]
    [InlineData("dueAt_asc")]
    public async Task OrderBy_Search_Verify_Output(string orderBy)
    {
        await FlowBuilder.For(Application)
            .AddOrderByDialogs()
            .ExecuteAndAssert(_ => { });

        var pages = await GetAllPages(orderBy);

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));
        settings.UseFileName($"OrderBySnapshotTests.{orderBy}");

        await Verify(pages, settings)
            .UseDirectory("Snapshots")
            .DontScrubDateTimes();
    }

    private async Task<List<PaginatedList<DialogDto>>> GetAllPages(string orderBy)
    {
        List<PaginatedList<DialogDto>> pages = [];
        bool hasNextPage;
        string? continuationToken = null;
        do
        {
            var previousToken = continuationToken;
            var page = await FlowBuilder.For(Application)
                .SearchEndUserDialogs(x =>
                {
                    x.Limit = 2;
                    x.Party = [IntegrationTestUser.DefaultParty];
                    x.ContinuationToken = ContinuationTokenSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>
                        .TryParse(previousToken, out var token) ? token : null;
                    x.OrderBy =
                        OrderSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>.TryParse(orderBy,
                            out var orderSet)
                            ? orderSet
                            : null;
                })
                .ExecuteAndAssert<PaginatedList<DialogDto>>();

            pages.Add(page);
            hasNextPage = page.HasNextPage;
            continuationToken = page.ContinuationToken;
        } while (hasNextPage);

        return pages;
    }
}

public static class OrderByExtensions
{
    private static DateTimeOffset DateAnchor => new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IFlowExecutor<CreateDialogResult> AddOrderByDialogs(this IFlowStep step)
    {
        IFlowExecutor<CreateDialogResult> executor = null!;
        for (var i = 0; i < 5; i++)
        {
            var addDays = i;
            executor = step.CreateSimpleDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = IntegrationTestUser.DefaultParty;

                x.Dto.CreatedAt = DateAnchor.AddDays(addDays);
                x.Dto.UpdatedAt = DateAnchor.AddDays(addDays);
                // This test will break in 2123 ᕕ(⌐■_■)ᕗ ♪♬
                x.Dto.DueAt = DateAnchor.AddYears(100 + addDays);
            });
        }

        return executor;
    }
}
