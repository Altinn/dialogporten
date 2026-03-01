using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using AwesomeAssertions;
using GetDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogEndUserContextTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Search_Returns_EndUserContext_Labels_With_Any_Label_Match() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive])
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                query.Party = [ctx.GetParty()];
                query.Label = [SystemLabel.Values.Archive, SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>((result, ctx) =>
                result.Items.Should().ContainSingle(item =>
                    item.DialogId == ctx.GetDialogId() &&
                    item.EndUserContextRevision != Guid.Empty &&
                    item.SystemLabels.Contains(SystemLabel.Values.Archive)));

    [Fact]
    public Task Search_Doesnt_Return_EndUserContext_Labels_Without_Label_Match() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive])
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                query.Party = [ctx.GetParty()];
                query.Label = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>((result, ctx) =>
                result.Items.Should().BeEmpty());

    [Fact]
    public Task Search_With_Label_Filter_Returns_Full_Page() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
            })
            .SearchServiceOwnerDialogEndUserContexts(query =>
            {
                query.Party = [TestUsers.DefaultParty];
                query.Label = [SystemLabel.Values.Archive];
                query.Limit = 2;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>(result =>
            {
                result.Items.Should().HaveCount(2);
                result.Items.Should().OnlyContain(x => x.SystemLabels.Contains(SystemLabel.Values.Archive));
            });

    [Fact]
    public async Task Search_With_Label_Filter_Paginates_Deterministically_With_ContinuationToken()
    {
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var oldestArchiveId = Guid.CreateVersion7();
        var middleArchiveId = Guid.CreateVersion7();
        var filteredOutId = Guid.CreateVersion7();
        var newestArchiveId = Guid.CreateVersion7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var ts = baseTime.AddMinutes(1);
                x.Dto.Id = oldestArchiveId;
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
                x.Dto.CreatedAt = ts;
            })
            .CreateSimpleDialog((x, _) =>
            {
                var ts = baseTime.AddMinutes(2);
                x.Dto.Id = middleArchiveId;
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
                x.Dto.CreatedAt = ts;
            })
            .CreateSimpleDialog((x, _) =>
            {
                var ts = baseTime.AddMinutes(3);
                x.Dto.Id = filteredOutId;
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
                x.Dto.CreatedAt = ts;
            })
            .CreateSimpleDialog((x, _) =>
            {
                var ts = baseTime.AddMinutes(4);
                x.Dto.Id = newestArchiveId;
                x.Dto.Party = TestUsers.DefaultParty;
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
                x.Dto.CreatedAt = ts;
            })
            .ExecuteAndAssert(_ => { });

        var firstPage = await FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(query =>
            {
                query.Party = [TestUsers.DefaultParty];
                query.Label = [SystemLabel.Values.Archive];
                query.Limit = 1;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>();

        firstPage.Items.Should().ContainSingle();
        firstPage.Items.Single().DialogId.Should().Be(newestArchiveId);
        firstPage.HasNextPage.Should().BeTrue();
        firstPage.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        var firstToken = ContinuationTokenSet<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>
            .TryParse(firstPage.ContinuationToken, out var parsedFirstToken)
                ? parsedFirstToken
                : throw new InvalidOperationException("Unable to parse first continuation token.");

        var secondPage = await FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(query =>
            {
                query.Party = [TestUsers.DefaultParty];
                query.Label = [SystemLabel.Values.Archive];
                query.Limit = 1;
                query.ContinuationToken = firstToken;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>();

        secondPage.Items.Should().ContainSingle();
        secondPage.Items.Single().DialogId.Should().Be(middleArchiveId);
        secondPage.Items.Select(x => x.DialogId).Should().NotIntersectWith(firstPage.Items.Select(x => x.DialogId));
        secondPage.HasNextPage.Should().BeTrue();
        secondPage.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        var secondToken = ContinuationTokenSet<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>
            .TryParse(secondPage.ContinuationToken, out var parsedSecondToken)
                ? parsedSecondToken
                : throw new InvalidOperationException("Unable to parse second continuation token.");

        var thirdPage = await FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(query =>
            {
                query.Party = [TestUsers.DefaultParty];
                query.Label = [SystemLabel.Values.Archive];
                query.Limit = 1;
                query.ContinuationToken = secondToken;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>();

        thirdPage.Items.Should().ContainSingle();
        thirdPage.Items.Single().DialogId.Should().Be(oldestArchiveId);
        thirdPage.Items.Select(x => x.DialogId).Should().NotIntersectWith(firstPage.Items.Select(x => x.DialogId));
        thirdPage.Items.Select(x => x.DialogId).Should().NotIntersectWith(secondPage.Items.Select(x => x.DialogId));
        thirdPage.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public Task Search_Without_Party_Returns_ValidationError() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(_ => { })
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Search_With_Multiple_Parties_Returns_Matching_Dialogs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                query.Party = [ctx.GetParty(), "urn:altinn:person:identifier-no:19895597581"];
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>((result, ctx) =>
                result.Items.Should().ContainSingle(item => item.DialogId == ctx.GetDialogId()));

    [Fact]
    public async Task Search_ContentUpdatedAfter_Filters_On_ContentUpdatedAt()
    {
        DateTimeOffset? contentUpdatedAfter = null;
        var newestDialogId = Guid.Empty;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2))
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .AssertResult<GetDialogDto>(x =>
            {
                contentUpdatedAfter = x.ContentUpdatedAt.AddMilliseconds(-1);
                newestDialogId = x.Id;
            })
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                query.Party = [ctx.GetParty()];
                query.ContentUpdatedAfter = contentUpdatedAfter;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>(result =>
                result.Items.Should().ContainSingle(x => x.DialogId == newestDialogId));
    }

    [Fact]
    public Task Search_Returns_All_System_Labels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive, SystemLabel.Values.MarkedAsUnopened])
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) => query.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>((result, ctx) =>
            {
                var labels = result.Items.Single(item => item.DialogId == ctx.GetDialogId()).SystemLabels;
                labels.Should().Contain(SystemLabel.Values.Archive);
                labels.Should().Contain(SystemLabel.Values.MarkedAsUnopened);
                labels.Distinct().Should().HaveCount(labels.Count);
            });
}
