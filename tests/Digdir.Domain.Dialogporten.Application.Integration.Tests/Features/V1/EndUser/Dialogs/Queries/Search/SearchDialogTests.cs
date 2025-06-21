using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests : ApplicationCollectionFixture
{
    public SearchDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Search_Dialog_With_Non_Default_SystemLabel_Should_Return_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.SystemLabel = SystemLabel.Values.Bin)
            .SearchEndUserDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.First().EndUserContext.SystemLabels
                    .Should().Contain(SystemLabel.Values.Bin));

    [Fact]
    public Task Search_New_Dialog_Should_Return_Empty_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchEndUserDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.First().EndUserContext.SystemLabels.Should()
                    .ContainSingle(x => x == SystemLabel.Values.Default));
}
