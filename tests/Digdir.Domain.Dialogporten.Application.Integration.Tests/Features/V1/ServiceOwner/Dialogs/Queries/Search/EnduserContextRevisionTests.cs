using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class EnduserContextRevisionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Search_Should_Populate_EnduserContextRevision()
    {
        string? serviceResource = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => serviceResource = x.Dto.ServiceResource)
            .SearchServiceOwnerDialogs(x => x.ServiceResource = [serviceResource!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.EnduserContextRevision != Guid.Empty));
    }
}
