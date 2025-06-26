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

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public async Task Search_Should_Populate_Obsolete_SystemLabel()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => party = x.Dto.Party)
            .SearchServiceOwnerDialogs(x => x.Party = [party!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.SystemLabel == SystemLabel.Values.Default));
    }
}
