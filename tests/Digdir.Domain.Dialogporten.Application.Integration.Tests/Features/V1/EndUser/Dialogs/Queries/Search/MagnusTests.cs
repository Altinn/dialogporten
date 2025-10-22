using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class MagnusTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Test1()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchEndUserDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                // x.Search = "Magnus";
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();
    }
}
