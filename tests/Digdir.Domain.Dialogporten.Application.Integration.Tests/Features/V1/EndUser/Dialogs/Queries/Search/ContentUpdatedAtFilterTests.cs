using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using FluentAssertions;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ContentUpdatedAtFilterTests : ApplicationCollectionFixture
{
    public ContentUpdatedAtFilterTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Filter_Search_On_Content_Updated_At_Before()
    {
        DateTimeOffset? contentUpdatedAt = null!;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .GetEndUserDialog()
            .AssertResult<DialogDto>(x => contentUpdatedAt = x.ContentUpdatedAt)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.ContentUpdatedBefore = contentUpdatedAt;
            })
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x =>
                x.Items.Should().HaveCount(1));
    }

    [Fact]
    public async Task Can_Filter_Search_On_Content_Updated_At_After()
    {
        DateTimeOffset? contentUpdatedAt = null!;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .GetEndUserDialog()
            .AssertResult<DialogDto>(x => contentUpdatedAt = x.ContentUpdatedAt)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.ContentUpdatedAfter = contentUpdatedAt;
            })
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x =>
                x.Items.Should().HaveCount(1));
    }
}
