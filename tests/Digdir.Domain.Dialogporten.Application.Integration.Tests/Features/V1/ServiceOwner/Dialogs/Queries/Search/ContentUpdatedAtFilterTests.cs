using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using FluentAssertions;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogDto;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

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
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x => contentUpdatedAt = x.ContentUpdatedAt)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .SearchServiceOwnerDialogs(x =>
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
            .CreateSimpleDialog(x =>
            {
                x.Dto.UpdatedAt = DateTimeOffset.Now.AddDays(-2);
                x.Dto.Party = Party;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.UpdatedAt = DateTimeOffset.Now.AddDays(-2);
                x.Dto.Party = Party;
            })
            .CreateSimpleDialog(x => x.Dto.Party = Party)
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x => contentUpdatedAt = x.ContentUpdatedAt)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.ContentUpdatedAfter = contentUpdatedAt;
            })
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x =>
                x.Items.Should().HaveCount(1));
    }
}
