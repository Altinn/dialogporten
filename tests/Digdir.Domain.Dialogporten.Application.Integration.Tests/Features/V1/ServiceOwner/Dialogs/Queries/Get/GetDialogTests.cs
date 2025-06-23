using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests : ApplicationCollectionFixture
{
    public GetDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Get_New_Dialog_Should_Return_Empty_SystemLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should()
                    .ContainSingle(x => x == SystemLabel.Values.Default));

    [Fact]
    public async Task Get_ReturnsSimpleDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel));
            });
    }

    [Fact]
    public async Task Get_ReturnsDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateComplexDialog(x => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel));
            });
    }
}
