using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
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
    public Task Search_Without_Party_Returns_ValidationError() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(_ => { })
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public async Task Search_ContentUpdatedAfter_Filters_On_ContentUpdatedAt()
    {
        DateTimeOffset? contentUpdatedAfter = null!;
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
