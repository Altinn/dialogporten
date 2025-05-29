using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeletedDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Fetching_Deleted_Dialog_Should_Return_Gone()
    {
        // Using FlowBuilder
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<EntityDeleted>(entityDeleted =>
            {
                entityDeleted.Should().NotBeNull();
                entityDeleted.Message.Should().Contain("is removed");
            });

    }
}
