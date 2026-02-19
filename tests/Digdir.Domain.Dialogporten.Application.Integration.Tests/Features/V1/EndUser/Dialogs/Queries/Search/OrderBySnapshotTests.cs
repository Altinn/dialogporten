using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

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
    public Task OrderBy_Search_Verify_Output(string orderBy) =>
        FlowBuilder.For(Application)
            .AddOrderByDialogs()
            .GetAllPages(orderBy)
            .VerifySnapshot(x =>
            {
                x.IgnoreMember(nameof(PaginatedList<>.ContinuationToken));
                x.UseFileName($"OrderBySnapshotTests.{orderBy}");
                x.DontScrubDateTimes();
            })
            .ExecuteAsync();
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
            executor = step.CreateSimpleDialog((x, _) =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
                x.Dto.Party = TestUsers.DefaultParty;

                x.Dto.CreatedAt = DateAnchor.AddDays(addDays);
                x.Dto.UpdatedAt = DateAnchor.AddDays(addDays);
                // This test will break in 2123 ᕕ(⌐■_■)ᕗ ♪♬
                x.Dto.DueAt = DateAnchor.AddYears(100 + addDays);
            });
        }

        return executor;
    }

    public static IFlowExecutor<List<PaginatedList<DialogDto>>> GetAllPages<TIn>(
        this IFlowStep<TIn> step,
        string orderBy) =>
        step.SelectAsync(async (_, context, cancellationToken) =>
        {
            List<PaginatedList<DialogDto>> pages = [];
            string? continuationToken = null;
            bool hasNextPage;

            do
            {
                var previousToken = continuationToken;
                var page = await context.Application.Send(new SearchDialogQuery
                {
                    Limit = 2,
                    Party = [TestUsers.DefaultParty],
                    ContinuationToken = ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>
                        .TryParse(previousToken, out var token)
                        ? token
                        : null,
                    OrderBy = OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(orderBy, out var orderSet)
                        ? orderSet
                        : null
                }, cancellationToken);

                var typedPage = page.Value.Should().BeOfType<PaginatedList<DialogDto>>().Subject;
                pages.Add(typedPage);
                continuationToken = typedPage.ContinuationToken;
                hasNextPage = typedPage.HasNextPage;
            } while (hasNextPage);

            return pages;
        });
}
