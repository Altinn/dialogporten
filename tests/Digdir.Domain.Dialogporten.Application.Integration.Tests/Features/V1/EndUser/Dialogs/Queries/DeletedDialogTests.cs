using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeletedDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Fetching_Deleted_Dialog_Should_Return_Gone() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>(entityDeleted =>
                entityDeleted.Message.Should().Contain("is removed"));
}
