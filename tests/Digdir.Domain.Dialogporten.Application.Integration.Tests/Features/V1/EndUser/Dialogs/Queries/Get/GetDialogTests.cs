using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests : ApplicationCollectionFixture
{
    public GetDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Get_New_Dialog_Should_Return_Empty_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should()
                    .ContainSingle(x => x == SystemLabel.Values.Default));
}
