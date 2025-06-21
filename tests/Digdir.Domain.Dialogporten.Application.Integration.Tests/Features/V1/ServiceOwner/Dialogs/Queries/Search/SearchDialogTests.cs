using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests : ApplicationCollectionFixture
{
    public SearchDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Search_Dialog_With_Non_Default_SystemLabel_Should_Return_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.SystemLabel = SystemLabel.Values.Bin)
            .SearchServiceOwnerDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.First().EndUserContext.SystemLabels
                    .Should().Contain(SystemLabel.Values.Bin));

    [Fact]
    public Task Search_New_Dialog_Should_Return_Empty_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchServiceOwnerDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.First().EndUserContext.SystemLabels.Should()
                    .ContainSingle(x => x == SystemLabel.Values.Default));

    [Fact]
    public Task Freetext_Search_With_Valid_SearchTerm_Returns_Success() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Search = "foobar";
                x.Party = [DialogGenerator.GenerateRandomParty()];
                x.EndUserId = DialogGenerator.GenerateRandomParty(forcePerson: true);
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

    [Fact]
    public Task Freetext_Search_Without_EndUserId_Results_In_ValidationError() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x => x.Search = "foobar")
            .ExecuteAndAssert<ValidationError>();
}
