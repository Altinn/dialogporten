using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeletedDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Fetching_Label_Assignment_Log_For_Deleted_Dialog_Should_Return_Gone() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>(entityDeleted =>
                entityDeleted.Message.Should().Contain("is removed"));
}
